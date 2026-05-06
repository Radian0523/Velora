using UnityEngine;
using Velora.Core;

namespace Velora.Weapon
{
    /// <summary>
    /// 地面に配置する弾薬ピックアップオブジェクト。
    /// トリガー接触でプレイヤーの WeaponController にリザーブ弾薬を補充し、自身を破棄する。
    /// WeaponPickup と同じ回転・浮遊演出で視認性を確保する。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private int _ammoAmount = 30;

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

        private void OnTriggerEnter(Collider other)
        {
            var weaponController = other.GetComponentInChildren<WeaponController>();
            if (weaponController == null) return;

            weaponController.AddReserveAmmo(_ammoAmount);
            EventBus.Publish(new AmmoPickedUpEvent());
            Destroy(gameObject);
        }
    }
}
