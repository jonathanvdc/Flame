#pragma once
#include <vector>
#include "CD.h"

class CDCatalog
{

private:
    std::vector<CD> Items;

public:
    std::vector<CD> GetItems() const;

    void AddItem(CD Value);

    CDCatalog();

    CDCatalog(std::vector<CD> Items);

};