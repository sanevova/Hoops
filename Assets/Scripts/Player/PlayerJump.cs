using UnityEngine;
using DG.Tweening;

public class PlayerJump : MonoBehaviour {
    [SerializeField] private float _jumpHeight;
    [SerializeField] private float _jumpDuration;

    private Sequence _jumpTweener;

    private void Update() { }

    public void Process(PlayerController player, Vector3 movementDirection) {
        if (!enabled) {
            return;
        }
        if (!Input.GetKeyDown(KeyCode.X)) {
            return;
        }
        if (_jumpTweener != null && _jumpTweener.IsActive()) {
            return;
        }
        var jumpLandingVector = movementDirection * player.Speed * _jumpDuration;
        _jumpTweener = player.transform.DOJump(
            endValue: player.transform.position + jumpLandingVector,
            jumpPower: _jumpHeight,
            numJumps: 1,
            duration: _jumpDuration
        ).SetEase(Ease.Linear);
    }
}
