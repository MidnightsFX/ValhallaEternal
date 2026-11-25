using UnityEngine;

namespace AesirLineage.ragnarok.bosses {
    // A teleport an attack fires as its projectile: the caster vanishes and reappears just past its target, facing
    // it, with an effect where it left and where it lands, and, given an area effect, a burst there that carries the
    // attack's damage.
    //
    // Attacks run on the caster's owner, which moves the caster itself; its position then reaches everyone else as
    // any move does.
    public class BlinkStrike : MonoBehaviour, IProjectile {
        public float m_beyondTarget = 3.5f;
        // How far the landing may be above or below the target before it lands on the caster's side instead.
        public float m_maxHeightChange = 4f;
        public EffectList m_leaveEffects = new EffectList();
        public EffectList m_arriveEffects = new EffectList();
        public GameObject m_arriveAoe;

        public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo) {
            try {
                Blink(owner, hitNoise, hitData, item, ammo);
            } finally {
                Destroy(gameObject);
            }
        }

        public string GetTooltipString(int itemQuality) => "";

        private void Blink(Character owner, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo) {
            if (owner == null || owner.m_nview == null || !owner.m_nview.IsOwner()) { return; }
            Character target = owner.GetBaseAI()?.GetTargetCreature();
            if (target == null) { return; }

            Vector3 targetAt = target.transform.position;
            Vector3 across = targetAt - owner.transform.position;
            across.y = 0f;
            Vector3 direction = across.sqrMagnitude > 0.01f ? across.normalized : owner.transform.forward;
            if (!TryLanding(targetAt + direction * m_beyondTarget, targetAt.y, out Vector3 landing)
                && !TryLanding(targetAt - direction * m_beyondTarget, targetAt.y, out landing)) {
                return;
            }

            Quaternion facing = Quaternion.LookRotation(-direction);
            m_leaveEffects.Create(owner.transform.position, owner.transform.rotation);
            owner.transform.SetPositionAndRotation(landing, facing);
            if (owner.m_body != null) {
                owner.m_body.position = landing;
                owner.m_body.linearVelocity = Vector3.zero;
            }
            m_arriveEffects.Create(landing, facing);
            if (m_arriveAoe != null && Instantiate(m_arriveAoe, landing, facing).GetComponent<Aoe>() is Aoe aoe) {
                aoe.Setup(owner, Vector3.zero, hitNoise, hitData, item, ammo);
            }
        }

        private bool TryLanding(Vector3 point, float targetHeight, out Vector3 landing) {
            landing = point;
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetSolidHeight(point, out float height)) { return false; }
            if (Mathf.Abs(height - targetHeight) > m_maxHeightChange) { return false; }
            landing.y = height + 0.2f;
            return true;
        }
    }
}
