using UnityEngine;
using PurrNet.Prediction;

public class FutureOfPlayerMovement : PredictedIdentity<FutureOfPlayerMovement.Input, FutureOfPlayerMovement.State>
{
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private float _moveForce = 5;


    protected override void Simulate(FutureOfPlayerMovement.Input input, ref State state, float delta)
    {
        // simulate is the local simulation/interpolation that occurs between packets from the server ig
        // all operations pertaining to state/input should pass through the 'interfaces' referenced by state and input here i.e. don't call input.get... in here
        Vector3 moveDirection = new Vector3(input.direction.x, input.direction.y).normalized * _moveForce;
        _rigidbody.AddForce(moveDirection);

    }

    protected override void GetFinalInput(ref Input input)
    {
        input.direction = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
    }

    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public Vector2 direction;

        public void Dispose() { }
    }

}
