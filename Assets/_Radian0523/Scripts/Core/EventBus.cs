using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// 型安全な static イベントバス。
    /// 発行側と購読側が互いの参照を持たずに通信する疎結合パターン。
    /// VContainer で注入するサービス間の 1対1 通信には DI を使い、
    /// 複数システムが同一イベントを購読する 1対N の横断的通知にはこの EventBus を使う。
    ///
    /// トレードオフ: 購読解除の漏れによるメモリリークに注意。
    /// MonoBehaviour の OnDestroy で必ず Unsubscribe すること。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                _handlers[type] = Delegate.Combine(existing, handler);
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                {
                    _handlers.Remove(type);
                }
                else
                {
                    _handlers[type] = result;
                }
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var handler))
            {
                ((Action<T>)handler)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// 全ハンドラを解除する。テストやシーン完全リセット時に使用。
        /// </summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }

    // --- イベント定義 ---
    // readonly struct で GC Alloc を回避しつつ、不変性を保証する

    public readonly struct PlayerDamagedEvent
    {
        public float Damage { get; }

        public PlayerDamagedEvent(float damage)
        {
            Damage = damage;
        }
    }

    public readonly struct PlayerDiedEvent { }

    public readonly struct WaveStartedEvent
    {
        public int WaveNumber { get; }

        public WaveStartedEvent(int waveNumber)
        {
            WaveNumber = waveNumber;
        }
    }

    public readonly struct WaveClearedEvent
    {
        public int WaveNumber { get; }

        public WaveClearedEvent(int waveNumber)
        {
            WaveNumber = waveNumber;
        }
    }

    public readonly struct WeaponFiredEvent { }

    public readonly struct UpgradeSelectedEvent { }

    public readonly struct EnemyDiedEvent
    {
        public int ScoreValue { get; }
        public Vector3 Position { get; }
        public string EnemyName { get; }

        public EnemyDiedEvent(int scoreValue, Vector3 position, string enemyName)
        {
            ScoreValue = scoreValue;
            Position = position;
            EnemyName = enemyName;
        }
    }

    public readonly struct EnemyDamagedEvent
    {
        public float Damage { get; }
        public Vector3 HitPosition { get; }
        public bool IsHeadshot { get; }

        public EnemyDamagedEvent(float damage, Vector3 hitPosition, bool isHeadshot)
        {
            Damage = damage;
            HitPosition = hitPosition;
            IsHeadshot = isHeadshot;
        }
    }
}
