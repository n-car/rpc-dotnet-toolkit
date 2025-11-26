namespace RpcServer.Example.Services;

public interface ICalculatorService
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    double Multiply(double a, double b);
    double Divide(double a, double b);
}

public class CalculatorService : ICalculatorService
{
    public int Add(int a, int b) => a + b;
    
    public int Subtract(int a, int b) => a - b;
    
    public double Multiply(double a, double b) => a * b;
    
    public double Divide(double a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        return a / b;
    }
}
