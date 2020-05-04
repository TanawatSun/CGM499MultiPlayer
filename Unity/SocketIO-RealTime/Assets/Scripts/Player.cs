using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool canControl = false;
    public float speedMove = 2.5f;
    public int maxHealth = 5;
    public int currentHealth;
    public HP_Controller healthBar;
    public bool die = false;
    [SerializeField] Camera cam;

    Vector2 mousePos;
    Rigidbody2D rb;
    Vector2 movement;

    private void Awake()
    {
        if (cam == null)
        {
            cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        }
        if (healthBar == null)
        {
            healthBar = GameObject.Find("HP_Bar").GetComponent<HP_Controller>();
        }
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!canControl)
            return;


        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        

        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        if(currentHealth<=0)
        {
            die = true;
        }

    }

    private void FixedUpdate()
    {
        if (!canControl)
            return;

        rb.MovePosition(rb.position + movement * speedMove * Time.fixedDeltaTime);

        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;

        rb.rotation = angle;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        FindObjectOfType<ConnectionManager>().hit = true;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
    }
}
