using Cysharp.Threading.Tasks;

namespace Velora.Enemy
{
    /// <summary>
    /// 敵の攻撃方式インターフェース（ストラテジーパターン）。
    /// WeaponController の IFireStrategy と同じ設計思想で、
    /// 新しい敵タイプ追加時に AttackState を変更せず Behavior クラスを1つ追加するだけで済む。
    /// </summary>
    public interface IAttackBehavior
    {
        UniTask Attack(EnemyController controller);
    }
}
