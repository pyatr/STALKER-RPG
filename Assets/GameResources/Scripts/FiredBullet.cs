using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FiredBullet : MonoBehaviour
{
    public GameObject shooter;
    public float maxDistance;
    public Vector2 startPoint;
    public Vector2 targetPoint;
    public BulletDamage bulletDamage = null;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Game.Instance.shootableLayers.Contains(collision.collider.gameObject.layer))
            CollideWithObject(collision.gameObject);
    }

    private void CollideWithObject(GameObject go)
    {
        //game.UpdateLog("hit " + go.name);
        Character collisionCharacterComponent = go.GetComponent<Character>();
        if (collisionCharacterComponent && bulletDamage != null)
            collisionCharacterComponent.TakeDamage(bulletDamage);
        DestorySelf();
    }

    public void DestorySelf()
    {
        if (gameObject == null)
            return;
        if (shooter != null)
        {
            shooter.GetComponent<Character>().movingBullets.Remove(gameObject);
            if (shooter.GetComponent<Character>().movingBullets.Count == 0)
                shooter.GetComponent<Character>().performingAction = false;
        }
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        float distance = Game.Instance.DistanceFromToInCells(startPoint, transform.position);
        if (!Game.Instance.PointIsOnScreen(transform.position) && !Game.Instance.PointIsOnScreen(targetPoint))
        {
            RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(transform.position, transform.up, maxDistance - distance);
            //Debug.DrawRay(transform.position, transform.up * (maxDistance - distance), Color.blue, 10);
            for (int i = 0; i < raycastHit2D.Length; i++)
                if (Game.Instance.shootableLayers.Contains(raycastHit2D[i].collider.gameObject.layer) && raycastHit2D[i].collider.gameObject != gameObject && raycastHit2D[i].collider.gameObject != shooter)
                    CollideWithObject(raycastHit2D[i].collider.gameObject);
            DestorySelf();
        }
        if (distance >= maxDistance)
            DestorySelf();
    }
}