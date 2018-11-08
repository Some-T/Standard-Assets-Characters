using UnityEngine;

namespace StandardAssets.Characters.Examples.SimpleMovementController
{
	[RequireComponent(typeof(CapsuleInput))]
	[RequireComponent(typeof(CharacterController))]
	public class DefaultCapsuleCharacter : MonoBehaviour
	{
		[SerializeField]
		protected float maxSpeed = 5f;

		[SerializeField]
		protected float timeToMaxSpeed = 0.5f;

		[SerializeField]
		protected float turnSpeed = 300f;
		
		[SerializeField]
		protected float groundCheckDistance = 0.51f;

		[SerializeField]
		protected LayerMask groundCheckMask;

		[SerializeField]
		protected float gravity = -9.81f;

		[SerializeField]
		protected float terminalVelocity = -100f;

		[SerializeField]
		protected float jumpSpeed = 10f;

		float movementTime, currentSpeed, airTime, currentVerticalVelocity, initialJumpVelocity, fallTime;

		bool previouslyHasInput;

		CapsuleInput characterInput;

		CharacterController controller;

		Transform mainCameraTransform;

		Vector3 verticalVector;

		void Awake()
		{
			characterInput = GetComponent<CapsuleInput>();
			controller = GetComponent<CharacterController>();
			mainCameraTransform = Camera.main.transform;
		}

		void OnEnable()
		{
			characterInput.jumpPressed += OnJump;
		}

		void OnJump()
		{
			initialJumpVelocity = jumpSpeed;
		}

		void OnDisable()
		{
			characterInput.jumpPressed -= OnJump;
		}

		void Update()
		{
			if (!characterInput.hasMovementInput)
			{
				return;
			}

			var flatForward = mainCameraTransform.forward;
			flatForward.y = 0f;
			flatForward.Normalize();

			var localMovementDirection = new Vector3(characterInput.moveInput.x, 0f, characterInput.moveInput.y);
			var cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
			cameraToInputOffset.eulerAngles = new Vector3(0f, cameraToInputOffset.eulerAngles.y, 0f);

			var targetRotation = Quaternion.LookRotation(cameraToInputOffset * flatForward);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
		}
		
		/// <summary>
		/// Handles movement on Physics update
		/// </summary>
		void FixedUpdate()
		{
			AerialMovement(Time.fixedDeltaTime);
			if (characterInput.hasMovementInput)
			{
				if (!previouslyHasInput)
				{
					movementTime = 0f;
				}
				Accelerate();
			}
			else
			{
				if (previouslyHasInput)
				{
					movementTime = 0f;
				}

				Stop();
			}

			var input = characterInput.moveInput;
			if (input.sqrMagnitude > 1)
			{
				input.Normalize();
			}
		
			var forward = transform.forward * input.magnitude;
			var sideways = Vector3.zero;
			
			controller.Move(((forward + sideways) * currentSpeed * Time.fixedDeltaTime) + verticalVector);

			previouslyHasInput = characterInput.hasMovementInput;
		}

		/// <summary>
		/// Calculates current speed based on acceleration anim curve
		/// </summary>
		void Accelerate()
		{
			movementTime += Time.fixedDeltaTime;
			movementTime = Mathf.Clamp(movementTime, 0f, timeToMaxSpeed);
			currentSpeed = movementTime / timeToMaxSpeed * maxSpeed;
		}
		
		/// <summary>
		/// Stops the movement
		/// </summary>
		void Stop()
		{
			currentSpeed = 0f;
		}

		bool CheckGrounded()
		{
			Debug.DrawRay(transform.position + controller.center, 
			              new Vector3(0,-groundCheckDistance * controller.height,0), Color.red);
			if (UnityEngine.Physics.Raycast(transform.position + controller.center, 
			                                -transform.up, groundCheckDistance * controller.height, groundCheckMask))
			{
				return true;
			}
			
			var xRayOffset = new Vector3(controller.radius,0f,0f);
			var zRayOffset = new Vector3(0f,0f,controller.radius);		
			
			for (var i = 0; i < 4; i++)
			{
				var sign = 1f;
				Vector3 rayOffset;
				if (i % 2 == 0)
				{
					rayOffset = xRayOffset;
					sign = i - 1f;
				}
				else
				{
					rayOffset = zRayOffset;
					sign = i - 2f;
				}
				Debug.DrawRay(transform.position + controller.center + sign * rayOffset, 
				              new Vector3(0,-groundCheckDistance * controller.height,0), Color.blue);

				if (UnityEngine.Physics.Raycast(transform.position + controller.center + sign * rayOffset,
				                                -transform.up,groundCheckDistance * controller.height, groundCheckMask))
				{
					return true;
				}
			}
			return false;
		}

		void AerialMovement(float deltaTime)
		{
			airTime += deltaTime;
			if (currentVerticalVelocity >= 0.0f)
			{
				currentVerticalVelocity = Mathf.Clamp(initialJumpVelocity + gravity * airTime, terminalVelocity,
				                                      Mathf.Infinity);
			}

			if (currentVerticalVelocity < 0.0f)
			{
				currentVerticalVelocity = Mathf.Clamp(gravity * fallTime, terminalVelocity, Mathf.Infinity);
				fallTime += deltaTime;
				if (CheckGrounded())
				{
					initialJumpVelocity = 0.0f;
					verticalVector = Vector3.zero;

					fallTime = 0.0f;
					airTime = 0.0f;
					return;
				}
			}

			verticalVector = new Vector3(0.0f, currentVerticalVelocity * deltaTime, 0.0f);
		}
	}
}