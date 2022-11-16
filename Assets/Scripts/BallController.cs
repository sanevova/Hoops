using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BallState {
    Dribbled,
    Held,
    BasketThrow,
    Free,
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

    [Header("Throwing At Basket")]
    [SerializeField] private Transform Hoop;
    [SerializeField] private float _basketThrowCurvature;
    [SerializeField] private float _basketThrowAirborneDuration;
    [SerializeField] private AudioClip _onScoreSound;

    private Rigidbody _rigidbody;
    private SphereCollider _ballCollider;
    private float _yVelocity;

    private static BallController _instance;

    public static BallController GetInstance() {
        return _instance;
    }

    private void Awake() {
        _instance = this;
        _rigidbody = GetComponent<Rigidbody>();
        _ballCollider = GetComponent<SphereCollider>();
    }

    void Start() {
        State = BallState.Dribbled;
    }

    void Update() {
        switch (State) {
            case BallState.Dribbled:
                Dribble();
                break;
            case BallState.Held:
                Hold();
                break;
            case BallState.BasketThrow:
                ThrowAtBasket();
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

    void Hold() {
        transform.position = LerpTo(PlayerHoldingTheBall.HoldLocation.position, _grabDuration);
    }

    void ThrowAtBasket() {
        PlayerHoldingTheBall = null;
        var ballPosition = LerpTo(Hoop.position, _basketThrowAirborneDuration);
        var arc = Vector3.up
            * _basketThrowCurvature
            * Mathf.Sin(_stateElapsedTime / _basketThrowAirborneDuration * Mathf.PI);

        var previousFramePosition = transform.position;
        transform.position = ballPosition + arc;
        _yVelocity = (transform.position.y - previousFramePosition.y) / Time.deltaTime;
    }

    private Vector3 LerpTo(Vector3 destination, float duration) {
        _stateElapsedTime += Time.deltaTime;
        var lerpRatio = _stateElapsedTime / duration;
        return Vector3.Lerp(_previousStatePosition, destination, lerpRatio);
    }

    private void WillStateChangeTo(BallState newState) {
        _previousStatePosition = transform.position;
        if (newState == BallState.Free) {
            // becoming free
            _rigidbody.isKinematic = false;
            _ballCollider.isTrigger = false;
            _rigidbody.velocity = Vector3.up * _yVelocity;
        } else if (State == BallState.Free) {
            // becoming in play
            _rigidbody.isKinematic = true;
            _ballCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other) {
        ProcessInsideBasketCollision(other);
    }

    private void ProcessInsideBasketCollision(Collider other) {
        if (!other.CompareTag("InsideBasket")) {
            return;
        }
        if (_yVelocity > 0) {
            // touching the net from below does not count
            return;
        }
        if (State != BallState.BasketThrow) {
            // only allow scoring when the ball has been thrown at basket
            return;
        }
        // scored
        AudioSource.PlayClipAtPoint(_onScoreSound, transform.position);
        State = BallState.Free;
    }
}
