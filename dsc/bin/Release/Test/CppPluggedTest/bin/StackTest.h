#pragma once
#include <memory>
#include "Stack.h"

class StackTest
{
public:
    StackTest();

private:
    std::shared_ptr<Stack<int>> x;

};