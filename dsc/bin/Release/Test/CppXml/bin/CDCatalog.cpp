#include <vector>
#include "CD.h"
#include "CDCatalog.h"

std::vector<CD> CDCatalog::GetItems() const
{
    return this->Items;
}

void CDCatalog::AddItem(CD Value)
{
    this->Items.push_back(Value);
}

CDCatalog::CDCatalog()
{ }

CDCatalog::CDCatalog(std::vector<CD> Items)
{
    this->Items = Items;
}