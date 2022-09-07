using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_Hitbox : MonoBehaviour, IHitDetector
{
    [SerializeField] private BoxCollider m_colider;
    [SerializeField] private LayerMask m_layerMask;


    private float m_thickness = 0.025f;
    private IHitResponder m_hitResponder;
    public IHitResponder HitResponder { get => m_hitResponder; set => m_hitResponder=value; }

    public void CheckHit()
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


        HitData _hitData = null;
        IHurtBox _hurtbox = null;


        RaycastHit[] _hits = Physics.BoxCastAll(_start, _halfExtends, _direction, _orientation, _distance, m_layerMask);
        
        foreach(RaycastHit _hit in _hits)
        {
            _hurtbox = _hit.collider.GetComponent<IHurtBox>();

            if(_hurtbox != null)
            {
                _hitData = new HitData
                {
                    damage = m_hitResponder == null ? 0 : m_hitResponder.Damage,
                    hitPoint = _hit.point == Vector3.zero ? _center : _hit.point,
                    hitNormal = _hit.normal,
                    hurtbox = _hurtbox,
                    hitDetector = this,
                    Sourse = transform
                };

                if(_hitData.Validate())
                {
                    _hitData.hitDetector.HitResponder?.Rensponse(_hitData);
                    _hitData.hurtbox.HurtResponder?.Rensponse(_hitData);
                }
            }
        }

    
    
    }
}
