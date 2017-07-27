public interface BaseEvent
{
    void Execute(World model, WorldView view);
}