from DefaultComparer import *
from IComparer import *

class MapComparer(IComparer):
    """ Compares items based on a mapping function. """

    def __init__(self, ItemMap):
        """ Creates a new map comparer. """
        self.actual_comparer = DefaultComparer()
        self.item_map = ItemMap

    def compare(self, Item, Other):
        """ Compares two items and returns an integer describing their relationship to each other. """
        # Pre:
        # 'Item' is the left operand of the comparison, 'Other' is the right operand.
        # Post:
        # Returns 0 if 'Item' and 'Other' are equal, -1 if 'Item' is less than 'Other' and 1 if 'Item' is greater than 'Other'.
        return self.actual_comparer.compare(self.item_map.map(Item), self.item_map.map(Other))

    @property
    def item_map(self):
        """ Gets the mapping function that is used to produce two comparable items. """
        return self.item_map_value

    @item_map.setter
    def item_map(self, value):
        """ Sets the mapping function that is used to produce two comparable items.
            This accessor is private. """
        self.item_map_value = value