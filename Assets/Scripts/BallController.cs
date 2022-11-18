using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
public enum BallState {
    Dribbled,
    ShiftDribble,
    Held,
    BasketThrow,
    Pass,
    Free,
    AfterScore,
}

public class BallController : MonoBehaviour {
    private BallState _state;
    private Vector3 _previousStatePosition;
    private float _stateElapsedTime;

    public BallState State {
        get { return _state; }
        set {
            if (_state != value) {
                _stateElapsedTime = 0;
                WillStateChangeTo(value);
            }
            _state = value;
        }
    }

    public PlayerController PlayerHoldingTheBall;
    [SerializeField] private float _bounceAmplitude;
    [SerializeField] private float _grabDuration;
    [SerializeField] private float _shiftDribbleDuration;
    private Tweener _shiftDribbleTweener;


    [Header("Throwing At Basket")]
    [SerializeField] private Transform _hoop;
    [SerializeField] private float _basketThrowCurvature;
    [SerializeField] private float _basketThrowAirborneDuration;
    [SerializeField] private float _basketThrowColliderShrinkFactor;

    [SerializeField] private AudioClip _onScoreSound;

    private Rigidbody _rigidbody;
    private SphereCollider _ballCollider;
    private Vector3 _calculatedVelocity;
    private Vector3 _passTargetPosition;

    private static BallController _instance;

    public static BallController GetInstance() {
        return _instance;
    }

    private void Awake() {
        _instance = this;
        _rigidbody = GetComponent<Rigidbody>();
        _ballCollider = GetComponent<SphereCollider>();
#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
        Debug.unityLogger.logEnabled = false;
#endif
    }

    void Start() {
        State = BallState.Dribbled;
    }

    void Update() {
        switch (State) {
            case BallState.Dribbled:
                Dribble();
                break;
            case BallState.ShiftDribble:
                ShiftDribble();
                Dribble();
                break;
            case BallState.Held:
                Hold();
                break;
            case BallState.BasketThrow:
                ThrowAtBasket();
                break;
            case BallState.Pass:
                Pass();
                break;
            default:
                break;
        }
    }


    void Dribble() {
        var yOffset = _bounceAmplitude * Mathf.Abs(Mathf.Sin(Time.time * 5));
        var nextDribblePosition = PlayerHoldingTheBall.DribbleStartLocation.position + Vector3.up * yOffset;
        if (_stateElapsedTime < _grabDuration) {
            transform.position = LerpTo(nextDribblePosition, _grabDuration);
        } else {
            transform.position = nextDribblePosition;
        }
    }
    private void ShiftDribble() {
        if (!IsFirstFrameOfState()) {
            return;
        }
        if (_shiftDribbleTweener != null && _shiftDribbleTweener.IsActive()) {
            return;
        }
        var dribbleLocation = PlayerHoldingTheBall.DribbleStartLocation;
        Debug.Log(dribbleLocation.localPosition.x);
        _shiftDribbleTweener = dribbleLocation.DOLocalMoveX(
            -dribbleLocation.localPosition.x,
            _shiftDribbleDuration);
    }

    private bool IsFirstFrameOfState() {
        return _stateElapsedTime == 0;
    }

    void Hold() {
        transform.position = LerpTo(PlayerHoldingTheBall.HoldLocation.position, _grabDuration);
    }

    void ThrowAtBasket() {
        Throw(_hoop.position, _basketThrowCurvature);
    }


    void Pass() {
        if (IsFirstFrameOfState()) {
            _passTargetPosition = PlayerHoldingTheBall.MousePosition;
        }
        Throw(_passTargetPosition, _basketThrowCurvature);
    }

    private void Throw(Vector3 targetPosition, float arcCurvature) {
        if (IsFirstFrameOfState()) {
            // refuse ball possession on the first frame of throwing
            PlayerHoldingTheBall = null;
        }

        var ballPosition = LerpTo(targetPosition, _basketThrowAirborneDuration);
        var arc = Vector3.up
            * arcCurvature
            * Mathf.Sin(_stateElapsedTime / _basketThrowAirborneDuration * Mathf.PI);

        var previousFramePosition = transform.position;
        transform.position = ballPosition + arc;
        _calculatedVelocity = (transform.position - previousFramePosition) / Time.deltaTime;
    }

    private Vector3 LerpTo(Vector3 destination, float duration) {
        _stateElapsedTime += Time.deltaTime;
        var lerpRatio = _stateElapsedTime / duration;
        return Vector3.Lerp(_previousStatePosition, destination, lerpRatio);
    }

    private void WillStateChangeTo(BallState newState) {
        _previousStatePosition = transform.position;
        if (newState == BallState.Free || newState == BallState.AfterScore) {
            // becoming free
            _rigidbody.isKinematic = false;
            _ballCollider.isTrigger = false;
            if (newState == BallState.Free) {
                _rigidbody.velocity = _calculatedVelocity;
            }
        } else if (State == BallState.Free || State == BallState.AfterScore) {
            // becoming in play
            _rigidbody.isKinematic = true;
            _ballCollider.isTrigger = true;
        }

        if (newState == BallState.BasketThrow) {
            _ballCollider.radius /= _basketThrowColliderShrinkFactor;
        } else if (State == BallState.BasketThrow) {
            _ballCollider.radius *= _basketThrowColliderShrinkFactor;
        }
        Debug.Log($"{State} -> {newState}");
    }

    private void OnTriggerEnter(Collider other) {
        CheckIfScored(other);
        TryPickUpBall(other);
        if (other.CompareTag("Rim") || other.CompareTag("Wall")) {
            other.isTrigger = false;
            if (State != BallState.Dribbled) {
                State = BallState.Free;
            }
        }
    }

    private void OnCollisionEnter(Collision other) {
        // collisions on ball movement
        TryPickUpBall(other.collider);
    }

    private void TryPickUpBall(Collider other) {
        if (!other.CompareTag("Player")) {
            return;
        }
        PlayerHoldingTheBall = other.gameObject.GetComponent<PlayerController>();
    }

    private void CheckIfScored(Collider other) {
        if (!other.CompareTag("InsideBasket")) {
            return;
        }
        if (_calculatedVelocity.y > 0) {
            // touching the net from below does not count
            return;
        }
        if (State == BallState.AfterScore) {
            // disallow double scoring;
            // needs to change to another state before can score again
            return;
        }
        // scored
        Score();
    }

    private void Score() {
        AudioSource.PlayClipAtPoint(_onScoreSound, transform.position);
        State = BallState.AfterScore;
        Debug.Log("Scored");
    }
}
