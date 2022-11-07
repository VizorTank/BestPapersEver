using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemFizician : MonoBehaviour
{
    [Header("Item")]
    public Item item;
    public int amount;

    [Header("Detection")]
    [SerializeField] private BoxCollider m_colider;
    [SerializeField] private LayerMask m_layer;
    private float m_thickness = 0.025f;

    [Header("Phisics")]
    public WorldClass worldClass;
    public bool isGrounded;
    public Vector3 DownBlock;
    public Vector3 VerticalMomentum = Vector3.zero;

    public void Start()
    {
        m_colider = transform.GetComponent<BoxCollider>();
        if (worldClass == null)
        {
            worldClass = GameObject.Find("World").transform.GetComponent<WorldClass>();
        }
    }

    public void Update()
    {
        ItemPhisics();
        CheckHit();
    }
    public void ItemPhisics()
    {
        if(!isGrounded)
        {
            VerticalMomentum += new Vector3(0, 1f, 0) * (-0.5f) * Time.deltaTime / 1;
        }
        else if(isGrounded)
        {
            VerticalMomentum = Vector3.zero;
        }
        
        
        FindGroundBlock();

        if (transform.position.y < DownBlock.y + 1)
        {
            transform.position = new Vector3(transform.position.x, DownBlock.y + 1, transform.position.z);
            isGrounded = true;
            VerticalMomentum = Vector3.zero;
        }
        else isGrounded = false;

        transform.position += VerticalMomentum;
    }

    private void FindGroundBlock()
    {

        Vector3 pos = transform.position;
        int type = 0;
        worldClass.TryGetBlock(pos, ref type);
        if (type != 0)
        {
            if (worldClass.blockTypesList.areSolid[type])
            {
                DownBlock = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y-0.1f), Mathf.Floor(pos.z));
                return;
            }
        }

        // step += checkIncrement;

        DownBlock = new Vector3(Mathf.Floor(transform.position.x), int.MinValue, transform.position.z);

    }

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


        RaycastHit[] _hits = Physics.BoxCastAll(_start, _halfExtends, _direction, _orientation, _distance, m_layer);

        foreach(RaycastHit hit in _hits)
        {
            if(hit.collider.gameObject.tag=="Player")
            {
                amount = hit.collider.GetComponent<PlayerController>().PickUpItem(new ItemStack(item, amount)).amount;
            }
            else if (!(amount == item.maxstack) && hit.collider.gameObject.CompareTag("Item"))
            {
                ItemFizician itemInteractable = hit.collider.gameObject.GetComponent<ItemFizician>();
                if (item == itemInteractable.item && amount < item.maxstack && amount >= itemInteractable.amount)
                {
                    if (itemInteractable.amount <= item.maxstack - amount)
                    {
                        amount += itemInteractable.amount;
                        itemInteractable.amount = 0;
                        Destroy(hit.collider.gameObject);
                    }
                    else
                    {
                        itemInteractable.amount -= item.maxstack - amount;
                        amount = item.maxstack;
                    }
                }
            }
            if (amount == 0)
                Destroy(gameObject);
        }



    }
}
