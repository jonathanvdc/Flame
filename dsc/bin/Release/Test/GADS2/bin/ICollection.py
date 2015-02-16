from IReadOnlyCollection import *

class ICollection(IReadOnlyCollection):
    """ Describes a generic collection that allows items to be added. """

    def add(self, Item):
        """ Adds an item to the collection. """
        # Remarks:
        # This method allows items to be added to a collection without knowledge of how the collection organizes itself.
        # It does not specify where the item will be inserted, only that it will be inserted.
        raise NotImplementedError("Method 'ICollection.add' was not implemented.")