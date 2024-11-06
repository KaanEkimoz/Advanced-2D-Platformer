using GlobalTypes;
using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    //Components
    private AdvancedCharacterCollision2D _characterCollision2D;
    private PlayerMovement _playerMovement;
    void Start()
    {
        _characterCollision2D = GetComponent<AdvancedCharacterCollision2D>();
        _playerMovement = GetComponent<PlayerMovement>();
    }
    void Update()
    {
        if (_characterCollision2D.ceilingType == GroundType.OneWayPlatform && _playerMovement._movementVector.y > 0f)
            StartCoroutine(DisableOneWayPlatform(_characterCollision2D.GetCeilingCollisionObject()));


        if (_characterCollision2D.groundType == GroundType.OneWayPlatform && PlayerInputHandler.Instance.IsPlayerPressingDownMovementButton())
            StartCoroutine(DisableOneWayPlatform(_characterCollision2D.GetGroundCollisionObject()));
    }

    IEnumerator DisableOneWayPlatform(GameObject currentOneWayPlatform)
    {
        currentOneWayPlatform.GetComponent<EdgeCollider2D>().enabled = false;

        yield return new WaitForSeconds(0.25f);

        currentOneWayPlatform.GetComponent<EdgeCollider2D>().enabled = true;
    }


}
