﻿using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CleanupHelper))]
public class Movement : MonoBehaviour {
   
    public float speed = 6.0F;                    // the speed of the character controller
    public float jumpSpeed = 8.0F;                // the vertical speed (or jumpSpeed) of the character controller
    public float gravity = 20.0F;                 // the gravity affecting the y movement vector of the character controller
    public AudioClip small_jump;                  // an audio clip storing the sound effect played when the character jumps
    public AudioClip spawn_sound;                 // an audio clip storing the sound effect played when the character spawns
    public float period = 0.13f;                  // animation timing 
    public Mesh[] runFrames = new Mesh[4];        // an array used to store the voxel models of the player run aniamtion
    public Mesh idleFrame;                        // a single Mesh variable used to hold a single idle frame for the character
    public Mesh jumpFrame;                        // a single Mesh variable used to hold a single jump frame for the character
    public Mesh death_frame;                      // a single Mesh variable used to hold a single death frame for the chracter
    public Color pipeSpawnColor;                  // a Color variable to be set in the inspector to set the tweened pipe color
    public string pipeTag;                        // meta data related to character spawn information
    public bool growTween;
    public AudioClip death_sound;                 // an audio clip storing the sound effect played when the character dies/perishes
    public AudioClip water_death_sound;           // an audio clip storing the sound effect played when the character drowns
    public ParticleSystem spawnParticle;          // a cached reference to the players spawn particle system
    public ParticleSystem jumpParticle;           // a cached reference to the players jump particle system
    public ParticleSystem walkParticles;

    private Vector3 moveDirection = Vector3.zero; // the player's movement vector for the CharacterController
    private CharacterController controller;       // a cached reference to the player's CharacterController
    private MeshFilter meshFilter;                // a cached reference to the player's MeshFilter
    private HFTInput hftInput;                    // a cached reference to the Happy Fun Times Input Manager                  
    private AudioSource audioSource;              // a cached reference to the player's AudioSource component
    private Camera mainCamera;
    private float nextFrameChange = 0.0f;         // an frame animation variable used to keep track of time passed relative to period
    private bool isStanding;                      // a boolean flag that indicates whether or not the player is standing
    public bool isDead = false;                   // a boolean flag that indicates whether or not the player is dead
    private int currentFrame;                     // an integer that stores the current frame in the frame/model based animation
    private bool onSpring = false;                // a boolean flag that indicates whether or not the player is jumping on a spring
    private GameObject walkParticleSystem;
	private float jTimer = 0f;

	public void Awake()
	{
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 45;
	}
    public void Start()
    {
		
		walkParticleSystem = (Instantiate(walkParticles, new Vector3(this.transform.position.x + this.transform.gameObject.GetComponent<CharacterController>().bounds.extents.x - .2f, this.transform.position.y - this.transform.GetComponent<CharacterController>().bounds.extents.y + .4f, this.transform.position.z), Quaternion.identity) as ParticleSystem).transform.gameObject;
		walkParticleSystem.transform.SetParent(this.gameObject.transform);
		walkParticleSystem.gameObject.SetActive(false);
		walkParticleSystem.transform.SetParent(this.gameObject.transform);

		Debug.Log ("Start Called");
        // Fancy code that grabs the first integer in the frame count
       Int32.TryParse(System.Text.RegularExpressions.Regex.Replace(this.GetComponent<MeshFilter>().mesh.name, @"[^\d]", ""), out currentFrame);
       
       currentFrame -= 1; // subtract one for array index accsess 
       spawnParticle.transform.position = transform.position;
       spawnParticle.enableEmission = true;
       jumpParticle.enableEmission = true;

       jumpParticle.playOnAwake = true;
       spawnParticle.playOnAwake = true;

       // cache frequently used components
       controller = GetComponent<CharacterController>();
       meshFilter = GetComponent<MeshFilter>();
       audioSource = GetComponent<AudioSource>();
       hftInput = GetComponent<HFTInput>();
       mainCamera = Camera.main;
       meshFilter.mesh = jumpFrame;
     
	   GameObject spawner = GameObject.FindWithTag(pipeTag);
	   transform.position = spawner.transform.position + spawner.transform.up * 3.0F;
	   moveDirection.y = jumpSpeed * spawner.transform.up.y;

       // Grab the child object of the pipe prefab and tween its color to pipeSpawnColor as set in the inspector
       spawner.transform.GetChild(0).GetComponent<Renderer>().material.DOColor(pipeSpawnColor, 0.5f);

       Sequence pipeSequence = DOTween.Sequence();
	   // stupid magica voxel and your crazy scaling smh
        if (growTween) {
			pipeSequence.Append(spawner.transform.DOScale(0.12f, 0.15f).SetEase(Ease.InOutBounce).SetLoops(1));
			pipeSequence.Append(spawner.transform.DOScale(0.08597419f, 0.5f).SetEase(Ease.InOutElastic).SetLoops(1));
		} else {
			pipeSequence.Append(spawner.transform.DOScale(0.05f, 0.5f).SetEase(Ease.InOutBounce).SetLoops(1));
			pipeSequence.Append(spawner.transform.DOScale(0.08597419f, 0.3f).SetEase(Ease.InOutElastic).SetLoops(1));
		}

       // after pipe tween effect play the spawn sound
	   audioSource.PlayOneShot (spawn_sound);
       // apply a small bounce effect to the character after spawn
       transform.DOScale(0.4f, 0.5f).SetEase(Ease.OutBounce).SetLoops(1);
       Destroy((Instantiate(spawnParticle, this.transform.position, Quaternion.identity) as ParticleSystem).transform.gameObject,1.5f);

    }
    void Update()
    {
        if (Input.GetAxis("Horizontal") > 0 || hftInput.GetAxis("Horizontal") > 0)
        {
            //meshFilter.mesh = runFrames[3];
            //nextFrameChange = 0.0f;
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            isStanding = false;
        }
        else if (Input.GetAxis("Horizontal") < 0 || hftInput.GetAxis("Horizontal") < 0)
        {
            //meshFilter.mesh = runFrames[3];
            //nextFrameChange = 0.0f;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            isStanding = false;
        }
        else 
        {
            isStanding = true;
        }

        if (controller.isGrounded)
        {
            if (isStanding)
            {
                meshFilter.mesh = idleFrame;
                walkParticleSystem.gameObject.SetActive(false);
            }
            else
            {
                walkParticleSystem.gameObject.SetActive(true);
            }

            moveDirection = new Vector3(hftInput.GetAxis("Horizontal") + Input.GetAxis("Horizontal"), 0, 0);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
			if(Input.GetKeyDown(KeyCode.Space))
			{
				jTimer = 0.09f;
			}

			if(jTimer > 0)
			{
				if(Input.GetKeyUp(KeyCode.Space) || hftInput.GetButtonUp("Fire1"))
				{
					jTimer = 0;
					audioSource.PlayOneShot(small_jump);
					Destroy((Instantiate(jumpParticle, this.transform.position, Quaternion.identity) as ParticleSystem).transform.gameObject, 1.5f); 
					meshFilter.mesh = jumpFrame;
					moveDirection.y = jumpSpeed * 0.75f;
				}else{
					jTimer -= Time.deltaTime;
					if (jTimer <= 0){
						jTimer = 0;
						audioSource.PlayOneShot(small_jump);
						Destroy((Instantiate(jumpParticle, this.transform.position, Quaternion.identity) as ParticleSystem).transform.gameObject, 1.5f); 
						meshFilter.mesh = jumpFrame;
						moveDirection.y = jumpSpeed;
					}
				}
			}

            if (onSpring)
            {
                audioSource.PlayOneShot(small_jump);
                meshFilter.mesh = jumpFrame;
                moveDirection.y = jumpSpeed * 1.5f;
                onSpring = false;

                Sequence cameraPushPop = DOTween.Sequence();
                cameraPushPop.easeOvershootOrAmplitude = 0.2f;
                cameraPushPop.Append(mainCamera.DOFieldOfView(50, 0.2f).SetEase(Ease.OutCirc));
                cameraPushPop.Append(mainCamera.DOFieldOfView(55, 0.35f).SetEase(Ease.InOutBack));
            }

        }
        else
        {
            walkParticleSystem.gameObject.SetActive(false);
            moveDirection = new Vector3(hftInput.GetAxis("Horizontal") + Input.GetAxis("Horizontal"), moveDirection.y, 0);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection.x *= speed;
        }

        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
        
        if (isDead != true)
        {
            if (Time.time > nextFrameChange)
            {
                nextFrameChange += period;
                if (isStanding == false &&
                    controller.isGrounded == true)
                {
                    runAnimation();
                }
            }
        }

        }
    private void runAnimation()
    {
        if (currentFrame <= 1)
            currentFrame += 1;
        else
            currentFrame = 0;

        this.GetComponent<MeshFilter>().mesh = runFrames[currentFrame];
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var normal = hit.normal;

        if (hit.gameObject.tag == "Spring")
        {
            if (hit.normal.y > 0.7f) {
                onSpring = true;

                Sequence springSquash = DOTween.Sequence();
                springSquash.Append(this.transform.DOScaleY(0.29f, 0.2f).SetEase(Ease.OutQuad));
                springSquash.Append(this.transform.DOScaleX(Math.Sign(this.transform.localScale.x) * 0.29f, 0.2f).SetEase(Ease.OutQuad));
                springSquash.Append(this.transform.DOScaleX(Math.Sign(this.transform.localScale.x) * 0.4f, 0.2f).SetEase(Ease.OutCubic));
                springSquash.Append(this.transform.DOScaleY(0.4f, 0.25f).SetEase(Ease.OutCubic));
               
                hit.gameObject.GetComponent<SpringAnimate>().AnimateSpring(0.09f);
                Destroy((Instantiate(jumpParticle, this.transform.position, Quaternion.identity) as ParticleSystem).transform.gameObject, 2.5f); 
            }
        }
        
        if (hit.gameObject.tag == "Death")
        {
            isDead = true;
            this.gameObject.GetComponent<MeshFilter>().mesh = death_frame;
            this.GetComponent<AudioSource>().PlayOneShot(water_death_sound);
            this.gameObject.transform.DOMove(new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y - 1f), 5f, false).OnComplete(this.gameObject.GetComponent<CleanupHelper>().WaitAndDestroy);
			Destroy(this);
        }

        if (hit.gameObject.GetComponent<Movement>() != null)
        {
            if ((hit.gameObject.tag == "Mario" || hit.gameObject.tag == "Luigi" || 
                hit.gameObject.tag == "PurpleMario" || hit.gameObject.tag == "YellowLuigi")
                && hit.normal.y > 0.7 && !hit.gameObject.GetComponent<Movement>().isDead && !isDead)
            {
                // simple messy edge case code

                hit.gameObject.GetComponent<Movement>().isDead = true;
                hit.gameObject.GetComponent<MeshFilter>().mesh = hit.gameObject.GetComponent<Movement>().death_frame;
                Destroy(hit.gameObject.GetComponent<Movement>());
                Sequence deathAnimation = DOTween.Sequence();
                hit.gameObject.GetComponent<AudioSource>().PlayOneShot(death_sound);
                deathAnimation.Append(hit.gameObject.transform.DOJump(new Vector3(hit.transform.position.x, hit.transform.position.y + 3f), 0.3f, 0, 0.4f, false).SetEase(Ease.InExpo));
                deathAnimation.Append(hit.gameObject.transform.DOMoveY(-12, 0.4f, false).SetEase(Ease.Linear));
                deathAnimation.OnComplete(hit.gameObject.GetComponent<CleanupHelper>().WaitAndDestroy);

            }
        }
    }



}
