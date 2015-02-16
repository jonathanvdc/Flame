class IMap:
    """ Describes a map, an object that maps source values to their target representation.
        It is essentially a pure mathematical function. """
    # Remarks:
    # This is mainly intended for use in sorting and dictionary purposes, where records are mapped to their keys.

    def map(self, Item):
        """ Maps the item to its target representation. """
        # Post:
        # This function must produce a constant return value, irrespective of external changes.
        raise NotImplementedError("Method 'IMap.map' was not implemented.")