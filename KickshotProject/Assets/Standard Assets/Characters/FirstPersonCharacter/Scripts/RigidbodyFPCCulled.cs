using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (Rigidbody))]
    [RequireComponent(typeof (CapsuleCollider))]
    public class RigidbodyFPCCulled : MonoBehaviour
    {
        public GameObject FacingDirection;
        public Camera Cam;
        public float TargetGroundSpeed = 2;
        public float AccelerationFactor = 1;
        public float DecelerationFraction = 0.05f;
        public float AirControl = 0.3f;
        public float KickSpeed = 50f;
        public float TurnSpeed = 0.4f;
        public MouseLook mouseLook = new MouseLook();
        
        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private Vector3 m_GroundContactNormal;
        private bool m_IsGrounded, m_CameraBackward;

        private Vector3 m_MoveDir;

        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init (transform, Cam.transform);
        }

        #region Update Functions

        private void Update()
        {
            RotateView();
            MovementInput();
            ActionInputs();
        }


        private void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation (transform, FacingDirection.transform);
            
        }

        private void MovementInput()
        {
            m_MoveDir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f).normalized;
        }

        private void ActionInputs()
        {
            if(Input.GetButtonDown("Fire1"))
            {
                ApplyKick(KickSpeed);
            }

            if(m_IsGrounded && Input.GetButtonDown("Jump"))
            {
                m_RigidBody.velocity += m_GroundContactNormal * 20;
            }

            m_CameraBackward = Input.GetButton("TurnAround");
        } 

        void ApplyKick(float Speed)
        {
            Vector3 kickVel = -1 * Cam.transform.forward * Speed;

            m_RigidBody.velocity += kickVel;
        }


        #endregion

        #region FixedUpdate functions

        private void FixedUpdate()
        {
            GroundCheck();
            ApplyMovement();
            TurnAroundUpdate();
        }


        private void GroundCheck()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius, Vector3.down, out hitInfo,
                                   ((m_Capsule.height / 2f) - m_Capsule.radius) + 0.1f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }
        }

        private void ApplyMovement()
        {
            Vector3 accelDir = FacingDirection.transform.forward * m_MoveDir.y + FacingDirection.transform.right * m_MoveDir.x;
            accelDir = Vector3.ProjectOnPlane(accelDir, m_GroundContactNormal).normalized;

            if (m_IsGrounded)
            {
                if (m_RigidBody.velocity.magnitude < TargetGroundSpeed)
                    m_RigidBody.velocity += accelDir * AccelerationFactor;
                ApplyFriction();
            }
            else
            {
                m_RigidBody.velocity += accelDir * AccelerationFactor * AirControl;
            }

            print(m_RigidBody.velocity);
        }

        private void ApplyFriction()
        {
            if (m_MoveDir == Vector3.zero)
            {
                Vector3 reducedVel = m_RigidBody.velocity - m_RigidBody.velocity.normalized * TargetGroundSpeed * DecelerationFraction;
                m_RigidBody.velocity = Vector3.Dot(reducedVel, m_RigidBody.velocity) < 0 ? Vector3.zero : reducedVel;
            }
            else if (m_RigidBody.velocity.magnitude > TargetGroundSpeed)
            {
                Vector3 reducedVel = m_RigidBody.velocity - m_RigidBody.velocity.normalized * TargetGroundSpeed * DecelerationFraction;
                m_RigidBody.velocity = m_RigidBody.velocity.magnitude < TargetGroundSpeed ? TargetGroundSpeed * m_RigidBody.velocity : reducedVel;
            }
        }

        private void TurnAroundUpdate()
        {
            Quaternion target = Quaternion.identity;
            if (m_CameraBackward)
                target = Quaternion.Euler(0, 180, 0);
            Cam.transform.localRotation = Quaternion.Lerp(Cam.transform.localRotation, target, TurnSpeed);
        }

        #endregion


    }
}
