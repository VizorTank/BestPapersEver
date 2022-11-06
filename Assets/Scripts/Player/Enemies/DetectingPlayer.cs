using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectingPlayer : MonoBehaviour
{
    [SerializeField] private SphereCollider m_colider;
    [SerializeField] private LayerMask m_layerMask;
    public EnemyAi enemyAi;
    private float m_thickness = 0.025f;

    public void CheckField()
    {

        if (enemyAi.target == null)
        {
            float _radius = m_colider.radius;
            float _distance = _radius - m_thickness;
            Vector3 _direction = transform.up;
            Vector3 _center = transform.TransformPoint(m_colider.center);
            Vector3 _start = _center - _direction * (_distance / 2);

            RaycastHit[] _hits = Physics.SphereCastAll(_start, _radius, _direction, _distance, m_layerMask);

            foreach (RaycastHit _hit in _hits)
            {
                if (_hit.collider.gameObject.transform.tag == "Player")
                {
                    enemyAi.target = _hit.collider.gameObject.transform;
                }
            }


        }
    }
}
