from IReadOnlySwapList import *
from IList import *

class SwapList(IReadOnlySwapList, IList):
    """ A basic implementation of a swap list. """
    # Remarks:
    # This class is essentially a wrapper around a list.
    # See 'IReadOnlySwapList<T>' for a more detailed swap list contract.

    def __init__(self, backingList):
        """ Creates a new swap list. """
        self.backing_list = backingList

    def swap(self, Container):
        """ Swaps the swap list's backing storage with the provided list. """
        # Pre:
        # 'Container' should be an empty list.
        # Post:
        # The swap list's backing list is set to list 'Container'.
        # If 'Container' is not empty, the swap list may clear it.
        while Container.count > 0:
            Container.remove_at(0)
        count = self.backing_list.count
        i = 0
        while i < count:
            Container.add(self.backing_list[i])
            i += 1
        self.backing_list = Container

    def add(self, Item):
        """ Adds an item to the collection. """
        self.backing_list.add(Item)

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, all items from the given index upward are shifted once to the right, and the provided item is inserted at the specified index in the list.
        return self.backing_list.insert(Index, Item)

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, the item at the specified index is removed and the index of all items whose index is greater than the provided index, will be decremented by one.
        return self.backing_list.remove_at(Index)

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.backing_list.__iter__()

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.backing_list.count

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the item at said index is returned.
        # Otherwise, an exception is thrown.
        return self.backing_list[Index]

    def __setitem__(self, Index, value):
        """ Sets the item at the specified position in the list. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        self.backing_list[Index] = value