using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FiredBullet : MonoBehaviour
{
    public GameObject shooter;
    public float maxDistance;
    public Vector2 startPoint;
    public BulletDamage bulletDamage = null;
    public Game game = null;
    //public bool startedFromCameraView = true;
    //public bool cameIntoCameraView = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (game.shootableLayers.Contains(collision.collider.gameObject.layer))
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
        float distance = game.DistanceFromToInCells(startPoint, transform.position);
        //if (!Game.PointIsOnScreen(transform.position) && startedFromCameraView && !cameIntoCameraView)
        //{
        //    RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(transform.position, transform.up, maxDistance - distance);
        //    Debug.DrawRay(transform.position, transform.up * (maxDistance - distance), Color.red, 10);
        //    for (int i = 0; i < raycastHit2D.Length; i++)            
        //        if (game.shootableLayers.Contains(raycastHit2D[i].collider.gameObject.layer) && raycastHit2D[i].collider.gameObject != gameObject)
        //            CollideWithObject(raycastHit2D[i].collider.gameObject);            
        //    DestorySelf();
        //}
        //else if (!startedFromCameraView)
        //    cameIntoCameraView = true;
        if (distance >= maxDistance)
            DestorySelf();
    }
}