using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Badin.Framework.Animations
{
	/// <summary>
	/// Controle de animações
	/// </summary>
	public class EasingControl : MonoBehaviour
	{
		/// <summary>
		/// Quando um "update tick" ocorre
		/// </summary>
		public event EventHandler UpdateEvent;
		/// <summary>
		/// Quando um evento muda de status
		/// </summary>
		public event EventHandler StateChangeEvent;
		/// <summary>
		/// Quando um evento se completa
		/// </summary>
		public event EventHandler CompletedEvent;
		/// <summary>
		/// Quando evento em loop é executado
		/// </summary>
		public event EventHandler LoopedEvent;

		/// <summary>
		/// Define como o controle irá atualizar a si mesmo
		/// </summary>
		public enum TimeType
		{
			/// <summary>
			/// Normal, é um loop de acordo com o Time.deltaTime, ou seja ele para de animar com o Time Scale em zero.
			/// </summary>
			Normal,
			/// <summary>
			/// Real, a animação continua animando mesmo com o Time Scale em zero.
			/// </summary>
			Real,
			/// <summary>
			/// Fixed, a animação é sincronizada com a engine de física.
			/// </summary>
			Fixed
		};

		/// <summary>
		/// Retorna o estado do controle da animação
		/// </summary>
		public enum PlayState
		{
			/// <summary>
			/// Parado
			/// </summary>
			Stopped,
			/// <summary>
			/// Pausado
			/// </summary>
			Paused,
			/// <summary>
			/// Animando
			/// </summary>
			Playing,
			/// <summary>
			/// Revertendo
			/// </summary>
			Reversing
		};

		/// <summary>
		/// Toda vez que a animação completa, se queremos que mantenha no lugar ou retorna a origem
		/// </summary>
		public enum EndBehavior
		{
			/// <summary>
			/// Mantém a posição onde terminar a animação
			/// </summary>
			Constant,
			/// <summary>
			/// Retorna ao ponto de origem ao terminar a aniamação
			/// </summary>
			Reset
		};

		/// <summary>
		/// Se o looping for habilitado, se queremos que repita do começo a cada loop ou inverta a animação para o começo.
		/// </summary>
		public enum LoopType
		{
			/// <summary>
			/// Reseta a animação ao terminar
			/// </summary>
			Repeat,
			/// <summary>
			/// Retorna revertendo a animação ao início
			/// </summary>
			PingPong
		};

		// Inicializando as variáveis

		/// <summary>
		/// Tipo da animação
		/// </summary>
		public TimeType timeType = TimeType.Normal;
		/// <summary>
		/// Estado atual da animação
		/// </summary>
		public PlayState CurrentPlayState { get; private set; }
		/// <summary>
		/// Estado anterior da animação
		/// </summary>
		public PlayState PreviousPlayState { get; private set; }
		/// <summary>
		/// Comportamento ao terminar a animação
		/// </summary>
		public EndBehavior endBehavior = EndBehavior.Constant;
		/// <summary>
		/// Tipo de looping
		/// </summary>
		public LoopType loopType = LoopType.Repeat;
		/// <summary>
		/// Se está durante uma animação
		/// </summary>
		public bool IsPlaying { get { return CurrentPlayState == PlayState.Playing || CurrentPlayState == PlayState.Reversing; } }


		/// <summary>
		/// Valor inicial
		/// </summary>
		public float startValue = 0.0f;
		/// <summary>
		/// Valor final
		/// </summary>
		public float endValue = 1.0f;
		/// <summary>
		/// Duração
		/// </summary>
		public float duration = 1.0f;
		/// <summary>
		/// Contagem de loops
		/// </summary>
		public int loopCount = 0;
		/// <summary>
		/// Interpolação atual
		/// </summary>
		public Func<float, float, float, float> equation = EasingEquations.Linear;

		/// <summary>
		/// O valor atual que varia entre zero e a duração especificada
		/// </summary>
		public float CurrentTime { get; private set; }
		/// <summary>
		/// O valor calculado pela EasingEquation em qualquer momento do tempo
		/// </summary>
		public float CurrentValue { get; private set; }

		public float CurrentOffset { get; private set; }
		public int Loops { get; private set; }

		/// <summary>
		/// Resume a animação ao habilitar o objeto
		/// </summary>
		private void OnEnable()
		{
			Resume();
		}

		/// <summary>
		/// Pausa a animação ao desabilitar o objeto
		/// </summary>
		private void OnDisable()
		{
			Pause();
		}

		public void Play()
		{
			SetPlayState(PlayState.Playing);
		}

		public void Reverse()
		{
			SetPlayState(PlayState.Reversing);
		}

		public void Pause()
		{
			SetPlayState(PlayState.Paused);
		}

		public void Resume()
		{
			SetPlayState(PreviousPlayState);
		}

		public void Stop()
		{
			SetPlayState(PlayState.Stopped);
			Loops = 0;
			if (endBehavior == EndBehavior.Reset)
				SeekToBeginning();
		}

		public void SeekToTime(float time)
		{
			CurrentTime = Mathf.Clamp01(time / duration);
			float newValue = (endValue - startValue) * CurrentTime + startValue;
			CurrentOffset = newValue - CurrentValue;
			CurrentValue = newValue;

			if (UpdateEvent != null)
				UpdateEvent(this, EventArgs.Empty);
		}

		public void SeekToBeginning()
		{
			SeekToTime(0.0f);
		}

		public void SeekToEnd()
		{
			SeekToTime(duration);
		}

		/// <summary>
		/// Alterna o tipo de PlayState
		/// </summary>
		/// <param name="target"></param>
		private void SetPlayState(PlayState target)
		{
			if (CurrentPlayState == target) return;

			PreviousPlayState = CurrentPlayState;
			CurrentPlayState = target;

			if (StateChangeEvent != null)
			{
				StateChangeEvent(this, EventArgs.Empty);
			}

			StopCoroutine("Ticker");
			if (IsPlaying) StartCoroutine("Ticker");
		}

		/// <summary>
		/// Corotina que controla o retorno do PlayState
		/// </summary>
		/// <returns></returns>
		IEnumerator Ticker()
		{
			while (true)
			{
				switch (timeType)
				{
					case TimeType.Normal:
						yield return new WaitForEndOfFrame();
						Tick(Time.deltaTime);
						break;
					case TimeType.Real:
						yield return new WaitForEndOfFrame();
						Tick(Time.unscaledDeltaTime);
						break;
					default: // Fixed
						yield return new WaitForFixedUpdate();
						Tick(Time.fixedDeltaTime);
						break;
				}
			}
		}

		/// <summary>
		/// Método que manipula os valores do update de acordo com a EasingEquation
		/// </summary>
		/// <param name="time"></param>
		private void Tick(float time)
		{
			bool finished = false;
			if (CurrentPlayState == PlayState.Playing)
			{
				CurrentTime = Mathf.Clamp01(CurrentTime + (time / duration));
				finished = Mathf.Approximately(CurrentTime, 1.0f);
			}
			else //Reversing
			{
				CurrentTime = Mathf.Clamp01(CurrentTime - (time / duration));
				finished = Mathf.Approximately(CurrentTime, 0.0f);
			}

			float frameValue = (endValue - startValue) * equation(0.0f, 1.0f, CurrentTime) + startValue;
			CurrentOffset = frameValue - CurrentValue;
			CurrentValue = frameValue;

			if (UpdateEvent != null)
				UpdateEvent(this, EventArgs.Empty);

			if (finished)
			{
				++Loops;
				if (loopCount < 0 || loopCount >= Loops)
					SeekToBeginning();
				else // PingPong
					SetPlayState(CurrentPlayState == PlayState.Playing ? PlayState.Reversing : PlayState.Playing);

				if (LoopedEvent != null)
					CompletedEvent(this, EventArgs.Empty);

				Stop();

			}
		}
	}
}