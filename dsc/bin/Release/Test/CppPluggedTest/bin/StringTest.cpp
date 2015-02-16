#include <string>
#include "StringTest.h"

StringTest::operator std::string() const
{
    return this->getA() + this->getB() + ". Why, hi there, C++ World!";
}

StringTest::StringTest(std::string A, std::string B)
{
    this->setA(A);
    this->setB(B);
}

std::string StringTest::getA() const
{
    return this->A_value;
}
void StringTest::setA(std::string value)
{
    this->A_value = value;
}

std::string StringTest::getB() const
{
    return this->B_value;
}
void StringTest::setB(std::string value)
{
    this->B_value = value;
}