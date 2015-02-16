#include "stdafx.h"
#include <memory>
#include "Stack.h"
#include "StackTest.h"

StackTest::StackTest()
{
    this->x = std::shared_ptr<Stack<int>>(new Stack<int>());
}