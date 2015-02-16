#pragma once
#include <memory>

class TestClass
{

private:
    int Value = 0;

public:
    int GetValue() const;

    std::shared_ptr<TestClass> GetThis();

    long long Pow(const int Exponent) const;

    TestClass Pow(const TestClass Exponent) const;

    TestClass(const int Value);

    TestClass operator+(const TestClass Other) const;

    TestClass operator*(const TestClass Other) const;

    TestClass();

};