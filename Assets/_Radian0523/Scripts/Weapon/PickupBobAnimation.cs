using UnityEngine;

namespace Velora.Weapon
{
    /// <summary>
    /// ピックアップオブジェクトの回転・上下浮遊アニメーション。
    /// WeaponPickup / AmmoPickup など、地面配置アイテムの視認性を高める共通演出。
    /// 個別のピックアップクラスから演出の責務を分離し、パラメータの一元管理を可能にする。
    /// </summary>
    public class PickupBobAnimation : MonoBehaviour
    {
        private const float RotationSpeed = 90f;
        private const float BobAmplitude = 0.15f;
        private const float BobFrequency = 1.5f;

        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, RotationSpeed * Time.deltaTime, Space.World);
            float bobOffset = Mathf.Sin(Time.time * BobFrequency * Mathf.PI * 2f) * BobAmplitude;
            transform.position = _startPosition + Vector3.up * bobOffset;
        }
    }
}
