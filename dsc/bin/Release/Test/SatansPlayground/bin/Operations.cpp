#include "Operations.h"

int Operations::f(int x) const
{
    int a = 2, b = 5, c = 9, result = x * (a + b - c);
    a = 6;
    return result * a;
}

int Operations::g(int x) const
{
    return x * -4;
}

Operations::Operations()
{ }


Vector2<int> Operations::Static_Singleton::ToVector(int A, int B) const
{
    return (Vector2<int>)Vector2<int>(A, B);
}

Operations::Static_Singleton::Static_Singleton()
{ }

std::shared_ptr<Operations::Static_Singleton> Operations::Static_Singleton::getInstance()
{
    if (Operations::Static_Singleton::Static_Singleton_instance_value == nullptr)
        Operations::Static_Singleton::Static_Singleton_instance_value = std::shared_ptr(new Operations::Static_Singleton());
    return Operations::Static_Singleton::Static_Singleton_instance_value;
}