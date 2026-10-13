public interface INestCarrySimulation
{
    NestCarryState Step(NestCarryState state, NestCarryInput input, float deltaTime);
}
