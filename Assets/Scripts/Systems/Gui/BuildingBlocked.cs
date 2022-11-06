using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBlocked : MonoBehaviour
{

    [SerializeField] private BoxCollider m_colider;
    private float m_thickness = 0.025f;

    public bool CheckBuilding()
    {
                Vector3 _scaledSize = new Vector3(
        m_colider.size.x * transform.localScale.x,
        m_colider.size.y * transform.localScale.y,
        m_colider.size.z * transform.localScale.z
        );

        float _distance = _scaledSize.y - m_thickness;
        Vector3 _direction = transform.up;
        Vector3 _center = transform.TransformPoint(m_colider.center);
        Vector3 _start = _center - _direction * (_distance / 2);
        Vector3 _halfExtends = new Vector3(_scaledSize.x, m_thickness, _scaledSize.z) / 2;
        Quaternion _orientation = transform.rotation;

        RaycastHit[] _hits = Physics.BoxCastAll(_start, _halfExtends, _direction, _orientation, _distance);


        if(_hits.Length!=1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
