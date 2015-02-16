from DoubleNode import *
from IList import *

class DoublyLinkedList(IList):
    """ A doubly linked list implementation of a list. """

    def __init__(self):
        """ Creates a new doubly linked list. """
        self.head = None

    def to_array(self):
        """ Gets an array representation of this linked list. """
        # Remarks:
        # This method was added to the 'DoublyLinkedList<T>' class to allow quick and easy O(n) iteration over its elements.
        # It goes without saying that this method should and can only be safely called if the 'DoublyLinkedList<T>' has been procured through a method that specifies its return value to be of said type.
        arr = [None] * self.count
        node = self.head
        i = 0
        while i < len(arr):
            arr[i] = node.value
            node = node.successor
            i += 1
        return arr

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        node = self.head
        while node is not None:
            yield node.value
            node = node.successor

    def node_at(self, Index):
        """ Gets the list node at the specified index. """
        # Pre:
        # The index must be valid index within this linked list: it must be non-negative and less than the list's length, exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the node at said index is returned.
        # Otherwise, None is returned.
        if Index < 0:
            return None
        node = self.head
        i = 0
        while node is not None and i < Index:
            node = node.successor
            i += 1
        return node

    def item_at(self, Index):
        """ Gets the item in the linked list at the given index. """
        # Pre:
        # The index must be valid index within this linked list: it must be non-negative and less than the list's length, exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the item at said index is returned.
        # Otherwise, an exception is thrown.
        return self.node_at(Index).value

    def add(self, Item):
        """ Adds an item to the end of the linked list. """
        # Pre:
        # Item is the item that will be inserted at the end of the list.
        # Post:
        # If the linked list has a head node, the item is inserted after the linked list's tail node.
        # Otherwise, a new head node containing Item is created, which coincidentally becomes this linked list's head.
        if self.head is None:
            self.head = DoubleNode(Item)
        else:
            self.tail.insert_after(Item)

    def remove_at(self, Index):
        """ Removes the item in the linked list at the specified index. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, the item at the specified index is removed and the index of all items whose index is greater than the provided index, will be decremented by one.
        if Index >= 0 and Index < self.count:
            if Index == 0:
                self.head.remove()
                self.head = self.head.successor
            else:
                node = self.node_at(Index)
                node.remove()
            return True
        else:
            return False

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        # Pre:
        # Index must be a valid index in the list: it must be non-negative and less than the list's length, as exposed by the Count property.
        # Post:
        # If the provided index was an invalid index in the list, the list's state is not changed, and false is returned.
        # Otherwise, all items from the given index upward are shifted once to the right, and the provided item is inserted at the specified index in the list.
        if Index == 0:
            oldHead = self.head
            self.head = DoubleNode(Item)
            self.head.set_successor(oldHead)
            return True
        node = self.node_at(Index - 1)
        if node is None:
            return False
        else:
            node.insert_after(Item)
            return True

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        node = self.head
        i = 0
        while node is not None:
            node = node.successor
            i += 1
        return i

    @property
    def tail(self):
        """ Gets the linked list's tail node.
            This accessor is private. """
        if self.head is None:
            return None
        else:
            return self.head.tail

    def __getitem__(self, Index):
        """ Gets the item in the linked list at the given index. """
        # Pre:
        # The index must be valid index within this linked list: it must be non-negative and less than the list's length, exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        return self.item_at(Index)

    def __setitem__(self, Index, value):
        """ Sets the item in the linked list at the given index. """
        # Pre:
        # The index must be valid index within this linked list: it must be non-negative and less than the list's length, exposed by the Count property.
        # Post:
        # If the index is a valid index in the linked list, the operation on said index is performed.
        # Otherwise, an exception is thrown.
        self.node_at(Index).value = value