#pragma once
#include <string>

class StringTest
{
public:
    operator std::string() const;

    StringTest(std::string A, std::string B);

    std::string getA() const;
    void setA(std::string value);

    std::string getB() const;
    void setB(std::string value);

private:
    std::string A_value;

    std::string B_value;

};