using UnityEngine;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 地面に配置する武器ピックアップオブジェクト。
    /// トリガー接触でプレイヤーの WeaponController に武器を追加し、自身を破棄する。
    /// 新しい武器を追加する際はこのプレハブを配置し、_weaponData に WeaponData SO を
    /// 設定するだけでよい（コード変更不要、データドリブン）。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponData _weaponData;

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
            // ピックアップオブジェクトの回転 + 上下浮遊で視認性を高める
            transform.Rotate(Vector3.up, RotationSpeed * Time.deltaTime, Space.World);
            float bobOffset = Mathf.Sin(Time.time * BobFrequency * Mathf.PI * 2f) * BobAmplitude;
            transform.position = _startPosition + Vector3.up * bobOffset;
        }

        /// <summary>
        /// CharacterController はトリガーとの接触で OnTriggerEnter を発火する。
        /// プレイヤー階層内の WeaponController を検索し、武器追加を試みる。
        /// 既に所持済みの武器の場合は AddWeapon が false を返すため、ピックアップは残る。
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            var weaponController = other.GetComponentInChildren<WeaponController>();
            if (weaponController == null) return;

            if (weaponController.AddWeapon(_weaponData))
            {
                Destroy(gameObject);
            }
        }
    }
}
