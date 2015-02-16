from IMap import *

class StackedMap(IMap):
    """ A stacked map is a map "stacks" two mapping function by applying a mapping function to the result of a first mapping function.
        A use case for this is when a key extracted from a record needs to be converted to its string representation. """

    def __init__(self, FirstMap, SecondMap):
        """ Creates a new stacked map instance from the first and second mapping functions. """
        self.first_map = FirstMap
        self.second_map = SecondMap

    def map(self, Value):
        """ Maps the item to its target representation. """
        # Post:
        # This function must produce a constant return value, irrespective of external changes.
        return self.second_map.map(self.first_map.map(Value))

    @property
    def second_map(self):
        """ Gets the stacked map's second mapping function. """
        return self.second_map_value

    @second_map.setter
    def second_map(self, value):
        """ Sets the stacked map's second mapping function.
            This accessor is private. """
        self.second_map_value = value

    @property
    def first_map(self):
        """ Gets the stacked map's first mapping function. """
        return self.first_map_value

    @first_map.setter
    def first_map(self, value):
        """ Sets the stacked map's first mapping function.
            This accessor is private. """
        self.first_map_value = value