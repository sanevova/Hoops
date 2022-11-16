using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Transform DribbleStartLocation;
    public Transform HoldLocation;

    [SerializeField] private float _speed;
    [SerializeField] private float _rotationSpeed;

    private CharacterController _characterController;
    private float _ySpeed;

    private BallController _ball;
    private PlayerDash _dash;

    void Start() {
        _characterController = GetComponent<CharacterController>();
        _dash = GetComponent<PlayerDash>();
        _ball = BallController.GetInstance();
    }

    void Update() {
        Vector3 movementDirection = new(-Input.GetAxis("Vertical"), 0, Input.GetAxis("Horizontal"));
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
            }
        }
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero) {
            RotateTowards(Quaternion.LookRotation(lookDirection, Vector3.up));
        }
        _dash.ProcessDash(transform, lookDirection);
        HandleBall();
    }

    private void RotateTowards(Quaternion dstRotation) {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, dstRotation, _rotationSpeed * Time.deltaTime);
    }

    private void HandleBall() {
        if (Input.GetKey(KeyCode.Space)) {
            _ball.PlayerHoldingTheBall = this;
        }
        if (!IsHoldingBall()) {
            return;
        }
        if (Input.GetKey(KeyCode.Space)) {
            _ball.State = BallState.Held;
        } else if (Input.GetKeyDown(KeyCode.Q) || Input.GetButtonDown("Fire1")) {
            _ball.State = BallState.BasketThrow;
        } else {
            _ball.State = BallState.Dribbled;
        }
    }

    private bool IsHoldingBall() {
        return _ball.PlayerHoldingTheBall == this;
    }
}
