public class Example
{
    public uint Fibonacci(uint i)
    {
        if (i <= 1)
            return i;

        return Fibonacci(i - 1) + Fibonacci(i - 2);
    }
}