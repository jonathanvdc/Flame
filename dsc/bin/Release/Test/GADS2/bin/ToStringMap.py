from IMap import *

class ToStringMap(IMap):
    """ A mapping function object that converts an argument to its string representation. """

    def __init__(self):
        """ Creates a new to-string map. """
        pass

    def map(self, Value):
        """ Maps the item to its target representation. """
        # Post:
        # This function must produce a constant return value, irrespective of external changes.
        return str(Value)