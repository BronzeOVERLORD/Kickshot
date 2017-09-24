﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public float KickSpeed = 50;
    public float FireDelay = 0.5f;
    public float StartGravity = 50;
    public float ChargeSpeed = 10;
    public bool WallKick;
    public float WallKickMultiplier = 1;
    public bool freeze;
    public GameObject BulletPoof;
    public AudioClip BulletSound;
    public bool WallRun;

    Transform _view_camera;
    Rigidbody _rigid_body;
    AudioSource _audio;
    

    float m_charge;
    bool _can_fire = true;

	// Use this for initialization
	void Start ()
    {
        WallRun = false;
        m_charge = 0;
        freeze = false;
        _view_camera = transform.GetChild(0);
        _rigid_body = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();

        Physics.gravity = new Vector3(0.0f, -StartGravity, 0.0f);
	}
	
	// Update is called once per frame
	void Update ()
    {
        if(_can_fire && Input.GetButtonDown("Fire1"))
        {
            FireProjectile(true);
            ApplyKick(false);
            StartCoroutine("ShootCooldown");
        }
        if (_can_fire && Input.GetButtonDown("Fire2"))
        {
            //FireProjectile(false);
            //ApplyKick(true);
            //StartCoroutine("ShootCooldown");
        }
        else
        {
            //ApplyKick(m_charge);
            m_charge = 0;
        }
        if (Input.GetButtonUp("Stick"))
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            freeze = false;
        }
        else if(Input.GetButtonDown("Stick") && WallRun == true)
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
            freeze = true;
        }
        
    }

    private void ChargeWeapon()
    {
        m_charge += ChargeSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Input.GetButton("Stick") && collision.collider.tag != "Ground")
        {
            if(WallKick)
            {
                GetComponent<Rigidbody>().velocity += (collision.contacts[0].normal + _view_camera.forward +_view_camera.up) * collision.relativeVelocity.magnitude * WallKickMultiplier;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if(Input.GetButton("Stick") && collision.collider.tag != "Ground" )
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
            freeze = true;
        }
        if(Input.GetButtonDown("Sprint") && collision.collider.tag == "Ground")
        {
            ApplyKick(KickSpeed, forward: true);
        }
    }

    // Launches the projectile
    void FireProjectile(bool forward)
    {
        int dirFactor = forward ? 1 : -1;

        RaycastHit bulletHit = new RaycastHit();
        if (Physics.Raycast(_view_camera.position, dirFactor * _view_camera.forward, out bulletHit) && forward)
        {
            GameObject poof = (GameObject)Instantiate(BulletPoof,bulletHit.point,Quaternion.LookRotation(bulletHit.normal));
            _audio.PlayOneShot(BulletSound);
        }
            // Debug.Log("Bullet Hit at:" + bulletHit.point);
    }

    // Applies the recoil to this GameObject's Rigidbody
    void ApplyKick(bool forward)
    {
        ApplyKick(KickSpeed, forward);
    }

    // Overloads ApplyKick with a custom 'Speed'
    void ApplyKick(float Speed, bool forward)
    {
        int dirFactor = forward ? 1 : -1;
        Vector3 kickVel = dirFactor * _view_camera.forward * Speed;
        Vector3 currentVel = GetComponent<Rigidbody>().velocity;

        // if (Vector3.Dot(kickVel, currentVel) < 0)
        //    currentVel = Vector3.zero;
        
        GetComponent<Rigidbody>().velocity = currentVel + kickVel;
    }

    IEnumerator ShootCooldown()
    {
        _can_fire = false;
        yield return new WaitForSeconds(FireDelay);
        _can_fire = true;
    }
}