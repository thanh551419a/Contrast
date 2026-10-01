using UnityEngine;

namespace Contrast.Player
{
    /// <summary>
    /// Jump/fall intent provider.
    /// Rigidbody2D is used only as motion state/configuration.
    /// It never moves the Player and never performs collision logic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerJumpMotion : MonoBehaviour
    {
        [Header("Jump")]
        [SerializeField, Min(0f)]
        private float jumpHeight = 3f;

        [Header("Fall")]
        [SerializeField, Min(0f)]
        private float maxFallSpeed = 16f;

        [Header("Debug")]
        [SerializeField] private bool logMotion = true;

        private Rigidbody2D rb;
        private bool jumping;

        public bool IsJumping => jumping;
        public float JumpHeight => jumpHeight;
        public float GravityScale => rb != null ? rb.gravityScale : 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.freezeRotation = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        public void RequestJump()
        {
            if (jumping)
                return;

            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            if (gravity <= Mathf.Epsilon || jumpHeight <= 0f)
                return;

            float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            Vector2 velocity = rb.linearVelocity;
            velocity.y = jumpVelocity;
            rb.linearVelocity = velocity;
            jumping = true;

            if (logMotion)
            {
                Debug.Log(
                    $"[JumpMotion] RequestJump velocityY={velocity.y:F4} gravity={gravity:F4}");
            }
        }

        public Vector2 GetMoveIntent(float deltaTime, bool isGrounded)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            if (isGrounded && !jumping)
            {
                SetVerticalVelocity(0f);
                return Vector2.zero;
            }

            float gravity = Physics2D.gravity.y * rb.gravityScale;
            Vector2 velocity = rb.linearVelocity;

            velocity.y += gravity * deltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            rb.linearVelocity = velocity;

            Vector2 intent = Vector2.up * (velocity.y * deltaTime);

            if (logMotion)
            {
                Debug.Log(
                    $"[JumpMotion] velocity={velocity} intent={intent} grounded={isGrounded} jumping={jumping}");
            }

            return intent;
        }

        public void NotifyGrounded(bool grounded)
        {
            if (!grounded)
                return;

            if (jumping && logMotion)
                Debug.Log("[JumpMotion] Grounded -> stop vertical motion");

            jumping = false;
            SetVerticalVelocity(0f);
        }

        public void ResetMotion()
        {
            jumping = false;
            SetVerticalVelocity(0f);
        }

        private void SetVerticalVelocity(float value)
        {
            if (rb == null)
                return;

            Vector2 velocity = rb.linearVelocity;
            velocity.y = value;
            rb.linearVelocity = velocity;
        }
    }
}
