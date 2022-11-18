using UnityEngine;

public class PlayerDash : MonoBehaviour {
    [SerializeField] private float _dashDistance;
    [SerializeField] private float _dashDuration;

    private Vector3 _dashStartPosition;
    private float _dashStartTime = -1000f;
    private Vector3 _dashDirection = Vector3.zero;

    private void Update() { }

    public void ProcessDash(Transform playerTransform, Vector3 lookDirection) {
        if (!enabled) {
            return;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift)) {
            _dashStartTime = Time.time;
            _dashStartPosition = playerTransform.position;
            _dashDirection = lookDirection;
        }
        var dashTimeElapsed = Time.time - _dashStartTime;
        if (dashTimeElapsed < _dashDuration) {
            playerTransform.position = Vector3.Lerp(
                _dashStartPosition,
                _dashStartPosition + _dashDistance * _dashDirection,
                Mathf.SmoothStep(0, 1, dashTimeElapsed / _dashDuration)
            );
        }
    }
}
