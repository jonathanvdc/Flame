#include <string>
#include "CD.h"

std::string CD::ToString() const
{
    return std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat(std::string::Concat("CD { Year: ", (std::string)this->getYear()), ", Title: "), this->getTitle()), ", Artist: "), this->getArtist()), ", Country: "), this->getCountry()), ", Company: "), this->getCompany()), ", Price: "), (std::string)this->getPrice()), " }");
}

CD::CD()
{ }

CD::CD(std::string Title, std::string Artist, std::string Country, std::string Company, int Year, double Price)
{
    this->setTitle(Title);
    this->setArtist(Artist);
    this->setCountry(Country);
    this->setCompany(Company);
    this->setYear(Year);
    this->setPrice(Price);
}

CD::CD(std::string Title, std::string Artist)
{
    this->setTitle(Title);
    this->setArtist(Artist);
}

int CD::getYear() const
{
    return this->Year_value;
}
void CD::setYear(int value)
{
    this->Year_value = value;
}

std::string CD::getTitle() const
{
    return this->Title_value;
}
void CD::setTitle(std::string value)
{
    this->Title_value = value;
}

std::string CD::getArtist() const
{
    return this->Artist_value;
}
void CD::setArtist(std::string value)
{
    this->Artist_value = value;
}

std::string CD::getCountry() const
{
    return this->Country_value;
}
void CD::setCountry(std::string value)
{
    this->Country_value = value;
}

std::string CD::getCompany() const
{
    return this->Company_value;
}
void CD::setCompany(std::string value)
{
    this->Company_value = value;
}

double CD::getPrice() const
{
    return this->Price_value;
}
void CD::setPrice(double value)
{
    this->Price_value = value;
}