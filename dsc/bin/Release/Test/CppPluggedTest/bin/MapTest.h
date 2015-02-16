#pragma once
#include <unordered_map>
#include <string>
#include <memory>
#include "Stack.h"

struct MapTest
{
    std::unordered_map<int, std::shared_ptr<Stack<std::string>>> vals;

    MapTest();

};