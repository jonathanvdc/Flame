class DateTime:
    """ Describes a date and time: a date and the time of day. """

    def __init__(self, Date, TimeOfDay):
        """ Creates a new date-time instance based on the date and time provided. """
        self.date = Date
        self.time_of_day = TimeOfDay

    def __str__(self):
        """ Gets the time's string representation. """
        return str(self.date) + " " + str(self.time_of_day)

    def __hash__(self):
        """ Gets a hash code for the current DateTime instance. """
        return hash(self.date) ^ hash(self.time_of_day)

    def __eq__(self, Other):
        """ Finds out if two 'DateTime' instances are equal. """
        return self.date == Other.date and self.time_of_day == Other.time_of_day

    def __ne__(self, Other):
        """ Finds out if two 'DateTime' instances are not equal. """
        return self.date != Other.date or self.time_of_day != Other.time_of_day

    def __lt__(self, Other):
        """ Finds out if this 'DateTime' instance is less than the given 'DateTime' instance. """
        if self.date < Other.date:
            return True
        elif self.date == Other.date:
            return self.time_of_day < Other.time_of_day
        else:
            return False

    def __gt__(self, Other):
        """ Finds out if this 'DateTime' instance is greater than the given 'DateTime' instance. """
        return not self <= Other

    def __le__(self, Other):
        """ Finds out if this 'DateTime' instance is less than or equal to the given 'DateTime' instance. """
        return self < Other or self == Other

    def __ge__(self, Other):
        """ Finds out if this 'DateTime' instance is greater than or equal to the given 'DateTime' instance. """
        return not self < Other

    @property
    def date(self):
        """ Gets this timestamp's date. """
        return self.date_value

    @date.setter
    def date(self, value):
        """ Sets this timestamp's date.
            This accessor is private. """
        self.date_value = value

    @property
    def time_of_day(self):
        """ Gets this timestamp's time of day. """
        return self.time_of_day_value

    @time_of_day.setter
    def time_of_day(self, value):
        """ Sets this timestamp's time of day.
            This accessor is private. """
        self.time_of_day_value = value