#pragma once
#include <memory>
#include "TestClass.h"

int TestClass::GetValue() const
{
    return this->Value;
}

std::shared_ptr<TestClass> TestClass::GetThis()
{
    return std::shared_ptr<TestClass>(this);
}

long long TestClass::Pow(const int Exponent) const
{
    int i = 0;
    long long result = 1;
    while (i < Exponent)
    {
        result *= this->Value;
        ++i;
    }
    return result;
}

TestClass TestClass::Pow(const TestClass Exponent) const
{
    int i = 0;
    TestClass result = TestClass(1);
    while (i < Exponent.Value)
    {
        result *= this->Value;
        ++i;
    }
    return result;
}

TestClass::TestClass(const int Value)
{
    this->Value = Value;
}

TestClass TestClass::operator+(const TestClass Other) const
{
    return TestClass(this->Value + Other.Value);
}

TestClass TestClass::operator*(const TestClass Other) const
{
    return TestClass(this->Value * Other.Value);
}

TestClass::TestClass()
{ }