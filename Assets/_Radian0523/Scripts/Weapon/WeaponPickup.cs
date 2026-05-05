using UnityEngine;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 地面に配置する武器ピックアップオブジェクト。
    /// トリガー接触でプレイヤーの WeaponController に武器を追加し、自身を破棄する。
    /// 既に所持済みの武器を拾った場合はマガジン1本分のリザーブ弾薬を補充する。
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
        /// 未所持の武器 → AddWeapon で追加。所持済み → マガジン分のリザーブ弾薬を補充。
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            var weaponController = other.GetComponentInChildren<WeaponController>();
            if (weaponController == null) return;

            if (weaponController.AddWeapon(_weaponData))
            {
                Destroy(gameObject);
                return;
            }

            // 所持済みの武器を拾った場合、マガジン1本分の弾薬をリザーブに補充する
            weaponController.AddReserveAmmo(_weaponData.MaxAmmo);
            Destroy(gameObject);
        }
    }
}
