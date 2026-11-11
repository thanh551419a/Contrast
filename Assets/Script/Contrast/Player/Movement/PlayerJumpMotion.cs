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
        // HTML authoritative physics constants (64 px = 1 world unit):
        // gravity = 1650 px/s^2 = 25.78125 units/s^2
        // jumpVel = 820 px/s = 12.8125 units/s -> jumpHeight = v^2 / (2*g) = 3.183712 units
        // maxFall = 1050 px/s = 16.40625 units/s
        public const float HtmlGravityMagnitude = 25.78125f;
        public const float HtmlJumpVelocity = 12.8125f;
        public const float HtmlJumpHeight = 3.183712f;
        public const float HtmlMaxFallSpeed = 16.40625f;

        [Header("Jump")]
        [SerializeField, Min(0f)]
        private float jumpHeight = HtmlJumpHeight;

        [Header("Fall")]
        [SerializeField, Min(0f)]
        private float maxFallSpeed = HtmlMaxFallSpeed;

        [Header("Debug")]
        [SerializeField] private bool logMotion = true;

        private Rigidbody2D rb;
        private bool jumping;
        private float verticalVelocity;

        public bool IsJumping => jumping;
        public float JumpHeight => jumpHeight;
        public float MaxFallSpeed => maxFallSpeed;
        public float GravityScale => rb != null ? rb.gravityScale : 0f;
        public float EffectiveGravityMagnitude => Mathf.Abs(ResolveGravity());
        public float CurrentVerticalVelocity => verticalVelocity;

        private void EnsureRigidbody()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            EnsureRigidbody();
            if (rb == null)
                return;

            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.freezeRotation = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            verticalVelocity = 0f;

            // Ensure default Rigidbody2D gravityScale matches HTML gravity (25.78125 units/s^2)
            float baseG = Mathf.Abs(Physics2D.gravity.y);
            if (baseG > Mathf.Epsilon && (Mathf.Approximately(rb.gravityScale, 1f) || rb.gravityScale <= 0f))
            {
                rb.gravityScale = HtmlGravityMagnitude / baseG;
            }
        }

        private float ResolveGravity()
        {
            EnsureRigidbody();
            if (rb == null)
                return -HtmlGravityMagnitude;

            float g = Physics2D.gravity.y * rb.gravityScale;
            if (Mathf.Abs(g) <= Mathf.Epsilon ||
                (Mathf.Approximately(rb.gravityScale, 1f) && Mathf.Approximately(Mathf.Abs(Physics2D.gravity.y), 9.81f)))
            {
                return -HtmlGravityMagnitude;
            }
            return g;
        }

        public void RequestJump()
        {
            EnsureRigidbody();
            float gravity = Mathf.Abs(ResolveGravity());
            if (gravity <= Mathf.Epsilon || jumpHeight <= 0f)
                return;

            float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            verticalVelocity = jumpVelocity;

            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.y = jumpVelocity;
                rb.linearVelocity = velocity;
            }
            jumping = true;

            if (logMotion)
            {
                Debug.Log(
                    $"[JumpMotion] RequestJump velocityY={jumpVelocity:F4} gravity={gravity:F4}");
            }
        }

        public void NotifyCeilingBlocked()
        {
            EnsureRigidbody();
            if (verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
            if (rb != null && rb.bodyType != RigidbodyType2D.Static && rb.linearVelocity.y > 0f)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }
        }

        public Vector2 GetMoveIntent(float deltaTime, bool isGrounded)
        {
            return GetMoveIntent(deltaTime, isGrounded, false);
        }

        public Vector2 GetMoveIntent(float deltaTime, bool isGrounded, bool fastFall)
        {
            EnsureRigidbody();
            deltaTime = Mathf.Max(0f, deltaTime);

            if (isGrounded && !jumping)
            {
                SetVerticalVelocity(0f);
                return Vector2.zero;
            }

            float gravity = ResolveGravity();
            Vector2 velocity = new Vector2(rb != null ? rb.linearVelocity.x : 0f, verticalVelocity);

            if (fastFall)
            {
                // HTML: vy += 900 * dt, which is 900 / 64 = ~14.0625 units/s^2 extra downward acceleration
                velocity.y -= 14.0625f * deltaTime;
            }

            velocity.y += gravity * deltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            verticalVelocity = velocity.y;
            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
                rb.linearVelocity = velocity;

            Vector2 intent = Vector2.up * (velocity.y * deltaTime);

            if (logMotion)
            {
                Debug.Log(
                    $"[JumpMotion] velocity={velocity} intent={intent} grounded={isGrounded} jumping={jumping} fastFall={fastFall}");
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
            verticalVelocity = value;
            EnsureRigidbody();
            if (rb == null || rb.bodyType == RigidbodyType2D.Static)
                return;

            Vector2 velocity = rb.linearVelocity;
            velocity.y = value;
            rb.linearVelocity = velocity;
        }
    }
}
