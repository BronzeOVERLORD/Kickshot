﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorGrapple: GunBase {
	public Transform gunBarrelFront;
	private LineRenderer linerender;
	private bool hitSomething = false;
	private Transform hitPosition;
	private float hitDist;
	private Vector3 lastPosition;
	private float fade = 0f;
	public float fadeTime = 1f;
	public float range = 12f;
	private Vector3 missStart;
	private Vector3 missEnd;
	private AudioSource shotSound;
	void Start() {
		// Copy a transform for use.
		hitPosition = Transform.Instantiate (gunBarrelFront);
		linerender = GetComponent<LineRenderer> ();
		linerender.enabled = false;
		shotSound = GetComponent<AudioSource> ();
	}
	override public void Update() {
		base.Update ();
		if (!equipped) {
			return;
		}
		transform.rotation = player.view.rotation;
		if (hitSomething) {
			// Keep us busy so we don't reload during grappling.
			busy = 1f;
			//player.transform.position = hitPosition.position - player.view.forward * hitDist;
			Vector3 desiredPosition = hitPosition.position - player.view.forward * hitDist;
			player.velocity = (desiredPosition - player.transform.position) / Time.deltaTime;
			lastPosition = player.transform.position;
			linerender.SetPosition (0, gunBarrelFront.position);
			linerender.SetPosition (1, hitPosition.position);
			fade = fadeTime;
			missStart = gunBarrelFront.position;
			missEnd = hitPosition.position;
		} else {
			if (fade > 0) {
				linerender.SetPosition (0, missStart);
				linerender.SetPosition (1, missEnd);
				fade -= Time.deltaTime;
			} else {
				linerender.enabled = false;
			}
		}
	}

	public override void OnPrimaryFire() {
		RaycastHit hit;
		// We ignore player collisions.
		if (Physics.Raycast (player.view.position, player.view.forward, out hit, range, ~(1 << LayerMask.NameToLayer ("Player")))) {
			hitPosition.SetParent (hit.collider.transform);
			hitPosition.position = hit.point;
			hitSomething = true;
			hitDist = hit.distance;
			lastPosition = player.transform.position;
			linerender.SetPosition (0, gunBarrelFront.position);
			linerender.SetPosition (1, hit.point);
		} else {
			hitSomething = false;
			fade = fadeTime;
			missStart = gunBarrelFront.position;
			missEnd = player.view.position + player.view.forward*range;
			linerender.SetPosition (0, missStart);
			linerender.SetPosition (1, missEnd);
		}
		linerender.enabled = true;
		shotSound.Play ();
	}

	public override void OnPrimaryFireRelease() {
		hitSomething = false;
	}
}
