using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Temp : MonoBehaviour
{
	public EasingControl ec;
	void Start()
	{
		ec = gameObject.AddComponent<EasingControl>();
		ec.startValue = -5;
		ec.endValue = 5;
		ec.duration = 3;
		ec.loopCount = -1; // inifinite looping
		ec.loopType = EasingControl.LoopType.PingPong;
		ec.equation = EasingEquations.EaseInOutQuad;
		ec.UpdateEvent += OnUpdateEvent;
		ec.Play();
	}

	void OnUpdateEvent(object sender, EventArgs e)
	{
		transform.localPosition = new Vector3(ec.CurrentValue, 0, 0);
	}
}