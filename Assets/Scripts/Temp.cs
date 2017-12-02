using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temp : MonoBehaviour
{
	public float value;

	void Start()
	{
		StartCoroutine("Tween");
	}

	IEnumerator Tween()
	{
		float start = -5;
		float end = 5;
		float duration = 3;
		float time = 0;
		while (time < duration)
		{
			yield return new WaitForEndOfFrame();
			time = Mathf.Clamp(time + Time.deltaTime, 0, duration);
			value = EasingEquations.EaseInOutQuad(start, end, (time / duration));
			transform.localPosition = new Vector3(value, 0, 0);
		}
	}
}
