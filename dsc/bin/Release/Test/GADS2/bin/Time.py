from Time import *

class Time:
    """ Represents the time of day. """

    def __init__(self, Hour, Minute, Second = None):
        """ Creates a new time based on an hour, minute and second. """
        # Pre:
        # Hour, minute and second must be non-negative integers.
        # Post:
        # Returns a Time instance that exactly represents the provided hour, minute and second.
        # Pre:
        # Hour and minute must be non-negative integers.
        # Post:
        # Returns a Time instance that exactly represents the provided hour and minute.
        if Second is None:
            self.total_seconds = 0
            self.total_seconds = Hour * 3600 + Minute * 60
            return
        self.total_seconds = 0
        self.total_seconds = Hour * 3600 + Minute * 60 + Second

    def to_double_digit_number(self, Value):
        """ Gets a double-digit string representation for the given integer.
            This method is private. """
        num = str(Value)
        if Value < 10:
            return "0" + num
        else:
            return num

    def __str__(self):
        """ Gets the time's string representation. """
        if self.second == 0:
            return str(self.hour) + ":" + self.to_double_digit_number(self.minute)
        else:
            return str(self.hour) + ":" + self.to_double_digit_number(self.minute) + ":" + self.to_double_digit_number(self.second)

    def __add__(self, Other):
        """ Calculates the sum of this 'Time' instance and the given 'Time' instance. """
        t = Time(0, 0)
        t.total_seconds = self.total_seconds + Other.total_seconds
        return t

    def __sub__(self, Other):
        """ Calculates the difference between this 'Time' instance and the given 'Time' instance. """
        t = Time(0, 0)
        t.total_seconds = self.total_seconds - Other.total_seconds
        return t

    def __eq__(self, Other):
        """ Finds out if two 'Time' instances are equal. """
        return self.total_seconds == Other.total_seconds

    def __ne__(self, Other):
        """ Finds out if two 'Time' instances are not equal. """
        return self.total_seconds != Other.total_seconds

    def __lt__(self, Other):
        """ Finds out if this 'Time' instance is less than the given 'Time' instance. """
        return self.total_seconds < Other.total_seconds

    def __gt__(self, Other):
        """ Finds out if this 'Time' instance is greater than the given 'Time' instance. """
        return self.total_seconds > Other.total_seconds

    def __le__(self, Other):
        """ Finds out if this 'Time' instance is less than or equal to the given 'Time' instance. """
        return self.total_seconds <= Other.total_seconds

    def __ge__(self, Other):
        """ Finds out if this 'Time' instance is greater than or equal to the given 'Time' instance. """
        return self.total_seconds >= Other.total_seconds

    def __hash__(self):
        """ Gets a hash code for the current Time instance. """
        return self.total_seconds

    @property
    def second(self):
        """ Gets the second of this time instance. """
        return self.total_seconds % 60

    @property
    def hour(self):
        """ Gets the hour of this time instance. """
        return self.total_seconds // 3600

    @property
    def minute(self):
        """ Gets the minute of this time instance. """
        return (self.total_seconds % 3600) // 60