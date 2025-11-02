using UnityEngine;
using Fusion.Addons.KCC;

namespace TPSBR
{
    public sealed class ClimbController : ContextBehaviour
    {
        [SerializeField]
        private LayerMask _climbableMask = ~0;

        [SerializeField]
        private float _enterDistance = 1.25f;

        [SerializeField]
        private float _exitDistance = 0.75f;

        [SerializeField]
        private float _climbSpeed = 2.5f;

        private readonly Collider[] _overlapResults = new Collider[8];

        private Character _character;
        private CharacterAnimationController _animationController;
        private KCC _kcc;
        private Health _health;
        private Ladder _activeLadder;
        private int _segmentIndex;
        private bool _isEndingClimb;

        public bool IsClimbing => _activeLadder != null && _isEndingClimb == false;

        private void Awake()
        {
            _character = GetComponent<Character>();
            _animationController = _character != null ? _character.AnimationController : null;
            _kcc = _character != null ? _character.CharacterController : null;
            _health = GetComponent<Health>();
        }

        public void ResetClimb()
        {
            if (_animationController != null)
            {
                _animationController.CancelLadderClimb();
            }

            _activeLadder = null;
            _segmentIndex = 0;
            _isEndingClimb = false;
        }

        public void UpdateClimbing(KCC kcc, ref GameplayInput input)
        {
            if (kcc == null || Runner == null)
            {
                return;
            }

            if (_health != null && _health.IsAlive == false)
            {
                if (_activeLadder != null)
                {
                    CancelActiveClimb();
                }

                return;
            }

            _kcc = kcc;

            if (_activeLadder != null)
            {
                UpdateActiveClimb(ref input);
            }
            else
            {
                TryStartClimb(ref input);
            }
        }

        private void TryStartClimb(ref GameplayInput input)
        {
            if (_animationController == null || _kcc == null)
            {
                return;
            }

            if (input.MoveDirection.y <= 0.1f)
            {
                return;
            }

            Vector3 origin = _kcc.FixedData.TargetPosition;
            float searchRadius = Mathf.Max(_enterDistance, 0.01f);
            int hitCount = Physics.OverlapSphereNonAlloc(origin, searchRadius, _overlapResults, _climbableMask, QueryTriggerInteraction.Collide);

            Ladder candidate = null;
            float bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hitCount; ++i)
            {
                Collider collider = _overlapResults[i];
                if (collider == null)
                {
                    continue;
                }

                Ladder ladder = collider.GetComponentInParent<Ladder>();
                if (ladder == null || ladder.WaypointCount < 2)
                {
                    continue;
                }

                float activationDistance = Mathf.Max(_enterDistance, ladder.ActivationDistance);
                float sqrDistance = (ladder.StartPoint - origin).sqrMagnitude;

                if (sqrDistance <= activationDistance * activationDistance && sqrDistance < bestDistanceSqr)
                {
                    bestDistanceSqr = sqrDistance;
                    candidate = ladder;
                }
            }

            if (candidate == null)
            {
                return;
            }

            if (_animationController.TryBeginLadderClimb(candidate) == false)
            {
                return;
            }

            _activeLadder = candidate;
            _segmentIndex = 0;
            _isEndingClimb = false;

            Vector3 startPoint = _activeLadder.StartPoint;
            _kcc.SetPosition(startPoint, true);
            AlignToSegment(_segmentIndex);

            input.MoveDirection = Vector2.zero;
            input.Jump = false;
        }

        private void UpdateActiveClimb(ref GameplayInput input)
        {
            if (_activeLadder == null || _animationController == null || _kcc == null)
            {
                return;
            }

            if (_activeLadder.isActiveAndEnabled == false)
            {
                CancelActiveClimb();
                return;
            }

            if (_activeLadder.WaypointCount < 2)
            {
                CancelActiveClimb();
                return;
            }

            if (_isEndingClimb == true)
            {
                bool stillPlaying = _animationController.UpdateLadderClimb(1f, false);
                if (stillPlaying == false)
                {
                    _activeLadder = null;
                    _isEndingClimb = false;
                }

                return;
            }

            Vector2 originalMove = input.MoveDirection;
            bool moveForward = originalMove.y > 0.1f;

            input.MoveDirection = Vector2.zero;
            input.Jump = false;

            AlignToSegment(_segmentIndex);

            Vector3 currentPosition = _kcc.FixedData.TargetPosition;
            Vector3 projected = _activeLadder.ProjectOnSegment(_segmentIndex, currentPosition);
            if ((projected - currentPosition).sqrMagnitude > 0.0001f)
            {
                _kcc.SetPosition(projected, false);
                currentPosition = projected;
            }

            int nextIndex = Mathf.Min(_segmentIndex + 1, _activeLadder.WaypointCount - 1);
            Vector3 nextPoint = _activeLadder.GetWaypointPosition(nextIndex);

            if (moveForward == true)
            {
                float step = Runner.DeltaTime * Mathf.Max(0.0f, _climbSpeed);
                Vector3 moved = Vector3.MoveTowards(currentPosition, nextPoint, step);
                _kcc.SetPosition(moved, false);
                currentPosition = moved;
            }

            float normalizedProgress = _activeLadder.GetNormalizedProgress(_segmentIndex, currentPosition);
            bool stateActive = _animationController.UpdateLadderClimb(normalizedProgress, moveForward);

            if (stateActive == false)
            {
                CancelActiveClimb();
                return;
            }

            float snapDistance = Mathf.Max(_activeLadder.WaypointSnapDistance, 0.05f);

            if (Vector3.Distance(currentPosition, nextPoint) <= snapDistance)
            {
                _kcc.SetPosition(nextPoint, true);
                currentPosition = nextPoint;

                if (_segmentIndex < _activeLadder.WaypointCount - 1)
                {
                    _segmentIndex++;
                }
            }

            float exitDistance = Mathf.Max(_activeLadder.ExitDistance, _exitDistance);
            if (Vector3.Distance(currentPosition, _activeLadder.EndPoint) <= exitDistance)
            {
                CompleteClimb();
            }
        }

        private void CompleteClimb()
        {
            if (_activeLadder == null || _animationController == null)
            {
                return;
            }

            _animationController.EndLadderClimb();
            _kcc.SetPosition(_activeLadder.EndPoint, true);
            _segmentIndex = Mathf.Max(0, _activeLadder.WaypointCount - 1);
            _isEndingClimb = true;
        }

        private void CancelActiveClimb()
        {
            _animationController?.CancelLadderClimb();
            _activeLadder = null;
            _segmentIndex = 0;
            _isEndingClimb = false;
        }

        private void AlignToSegment(int segmentIndex)
        {
            if (_activeLadder == null || _kcc == null)
            {
                return;
            }

            Vector3 direction = _activeLadder.GetSegmentDirection(segmentIndex);
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = _kcc.FixedData.LookPitch;
            _kcc.SetLookRotation(pitch, yaw);
        }
    }
}
