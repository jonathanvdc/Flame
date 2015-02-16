from IReadOnlyList import *
from ICollection import *

class IList(IReadOnlyList, ICollection):
    """ Describes a generic list. """
    # Remarks:
    # This list ADT inherits the 'Add(in Item : T)' method from Collection, with the added requirement that items inserted through Add should be placed at the end of the list.

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, all items from the given index upward are shifted once to the right, and the provided item is inserted at the specified index in the list.
        raise NotImplementedError("Method 'IList.insert' was not implemented.")

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, the item at the specified index is removed and the index of all items whose index is greater than the provided index, will be decremented by one.
        raise NotImplementedError("Method 'IList.remove_at' was not implemented.")

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        raise NotImplementedError("Method 'IList.__getitem__' was not implemented.")

    def __setitem__(self, Index, value):
        """ Sets the item at the specified position in the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        raise NotImplementedError("Method 'IList.__setitem__' was not implemented.")