﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementController : MonoBehaviour {
	public float rotationSpeed = 50.0f;

	public float zRotationSpeed = 20.0f;

    public float maxAngularVelocity = 3.0f;
#if UNITY_EDITOR
    public int mousePlayerIndex = 0;
#endif

    private void OnEnable()
    {
        GameManager.onGameEnd += OnGameEnd;
    }

    private void OnDisable()
    {
        GameManager.onGameEnd -= OnGameEnd;
    }

    private void OnGameEnd()
    {
        Destroy(gameObject);
    }

    // Update is called once per frame
    private void FixedUpdate () {
        GetComponent<Rigidbody>().maxAngularVelocity = maxAngularVelocity;
        transform.RotateAround (transform.position, Vector3.forward, zRotationSpeed * Time.fixedDeltaTime); 
		//transform.Rotate (Vector3.right * 60.0f * Time.fixedDeltaTime);
	}

    private void Update()
    {
        GetComponent<Animator>().SetInteger("Score", GameManager.instance.playerScore);
        //GetComponent<Animator>().SetBool("1P", GameManager.instance.gameMode == GameMode.P1);
    }

	public void OnHit(Vector3 cameraForward, RaycastHit hit, Vector3 rotationVector) {
		GetComponent<Rigidbody> ().AddForceAtPosition (cameraForward * rotationSpeed, hit.point, ForceMode.Impulse);
	}
}