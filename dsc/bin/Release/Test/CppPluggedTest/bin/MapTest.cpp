#include <unordered_map>
#include <string>
#include <memory>
#include "Stack.h"
#include "MapTest.h"

MapTest::MapTest()
{
    this->vals = (std::unordered_map<int, std::shared_ptr<Stack<std::string>>>)std::unordered_map<int, Stack<std::string>>();
}