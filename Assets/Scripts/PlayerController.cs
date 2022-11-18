using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    [SerializeField] private bool _isThisClientPlayer;
    public Transform DribbleStartLocation;
    public Transform HoldLocation;

    [SerializeField] private float _speed;
    [SerializeField] private float _rotationSpeed;
    [HideInInspector] public Vector3 MousePosition { get; private set; }

    private CharacterController _characterController;
    private float _ySpeed;

    private BallController _ball;
    private PlayerDash _dash;

    void Start() {
        _characterController = GetComponent<CharacterController>();
        _dash = GetComponent<PlayerDash>();
        _ball = BallController.GetInstance();
    }

#if UNITY_EDITOR
    private static GameObject _debugSphere;
#endif

    void Update() {

        Vector3 movementDirection = new(-Input.GetAxis("Vertical"), 0, Input.GetAxis("Horizontal"));
        if (!_isThisClientPlayer) {
            movementDirection = Vector3.zero;
        }
        movementDirection.Normalize();
        Vector3 velocity = movementDirection * _speed;

        _ySpeed += Physics.gravity.y * Time.deltaTime;
        if (_characterController.isGrounded) {
            _ySpeed = -1;
        }
        velocity.y = _ySpeed;

        _characterController.Move(velocity * Time.deltaTime);

        Vector3 lookDirection = movementDirection;
        if (movementDirection == Vector3.zero) {
            // standing still
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                if (hit.collider.name == "Plane") {
                    lookDirection = (hit.point - transform.position).normalized;
                }
                if (_isThisClientPlayer) {
                    MousePosition = hit.point;
#if UNITY_EDITOR
                    if (_debugSphere == null) {
                        _debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        _debugSphere.GetComponent<Collider>().enabled = false;
                        _debugSphere.transform.localScale = Vector3.one;
                    }
                    _debugSphere.transform.position = MousePosition;
#endif
                }

            }
        }
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero) {
            RotateTowards(Quaternion.LookRotation(lookDirection, Vector3.up));
        }
        if (_isThisClientPlayer) {
            _dash.ProcessDash(transform, lookDirection);
        }
        HandleBall();
    }

    private void RotateTowards(Quaternion dstRotation) {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, dstRotation, _rotationSpeed * Time.deltaTime);
    }

    private void HandleBall() {
        if (Input.GetKey(KeyCode.Space) && _isThisClientPlayer) {
            // summon ball cheat
            _ball.PlayerHoldingTheBall = this;
        }
        if (!IsHoldingBall()) {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetButtonDown("Fire1")) {
            _ball.State = BallState.BasketThrow;
        } else if (Input.GetKeyDown(KeyCode.R)) {
            _ball.State = BallState.Pass;
        } else if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) {
            _ball.State = BallState.Held;
        } else if (Input.GetKeyDown(KeyCode.Z)) {
            _ball.State = BallState.ShiftDribble;
        } else {
            _ball.State = BallState.Dribbled;
        }
    }

    private bool IsHoldingBall() {
        return _ball.PlayerHoldingTheBall == this;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        // collisions on characterController.Move()
        if (hit.collider.CompareTag("Ball")) {
            _ball.PlayerHoldingTheBall = this;
        }
    }
}
